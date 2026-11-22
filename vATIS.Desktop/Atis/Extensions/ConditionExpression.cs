// <copyright file="ConditionExpression.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// A condition on controllers online, the time, the weather and the runways in use, written as one piece of text,
/// e.g. <c>dep:07 AND EPKK_E_APP AND NOT ceiling &lt; 200ft</c>.
/// </summary>
/// <remarks>
/// Grammar (keywords are not case-sensitive, and AND binds tighter than OR):
/// <code>
/// list      := or ( [,;] or )*
/// or        := and ( OR and )*
/// and       := not ( AND not | NOT not )*
/// not       := NOT not | primary
/// primary   := '(' list ')' | time-range | runway | weather | callsign
/// time-range := hhmm [z] '-' hhmm [z]
/// runway    := ( dep: | arr: | rwy: ) runway-name
/// weather   := numeric-field ( '&lt;' | '&lt;=' | '&gt;' | '&gt;=' | '=' ) number [unit]
///            | text-field ( INCLUDES | EXCLUDES ) codes
///            | flag-field
/// callsign  := any other word
/// </code>
/// A callsign is true when any online controller matches it, and * matches any characters. A time range is true from
/// its start up to, but not including, its end, and wraps past midnight when the end is before the start. A runway is
/// true when it is one of the preset's runways: dep: looks at the departure runways, arr: at the arrival runways and
/// rwy: at both. A word that is a weather field name, or that is followed by a comparison, is a weather term.
/// Callsigns, runways and times that are written one after another with nothing between them are an implicit OR, so a
/// plain list of callsigns means any of them, and a NOT straight after one of them means AND NOT.
/// Weather terms are not met when there is no METAR, and so are not their negation. When that decides the outcome the
/// whole condition is not met.
/// </remarks>
public sealed class ConditionExpression
{
    private static readonly Regex s_timeRange = new(
        @"\G(\d{4})\s*[zZ]?\s*-\s*(\d{4})\s*[zZ]?(?![A-Za-z0-9_*])", RegexOptions.CultureInvariant);

    private static readonly Regex s_singleTime = new(@"^\d{4}[zZ]?-?$", RegexOptions.CultureInvariant);

    private readonly Matcher _root;

    private ConditionExpression(Matcher root, Parser parser)
    {
        _root = root;
        HasTimeCondition = parser.HasTime;
        HasCallsignCondition = parser.HasCallsign;
        HasWeatherCondition = parser.HasWeather;
        HasRunwayCondition = parser.HasRunway;
    }

    // Null is "unknown", which only a weather term can be, because there may be no METAR to read.
    private delegate bool? Matcher(ConditionContext context);

    private enum TokenKind
    {
        Word,
        Quoted,
        Operator,
        Time,
        Comma,
        Open,
        Close,
        End,
    }

    /// <summary>
    /// Gets a value indicating whether the condition depends on the time of day, so it needs checking again as time
    /// passes.
    /// </summary>
    public bool HasTimeCondition { get; }

    /// <summary>
    /// Gets a value indicating whether the condition depends on which controllers are online.
    /// </summary>
    public bool HasCallsignCondition { get; }

    /// <summary>
    /// Gets a value indicating whether the condition depends on the METAR.
    /// </summary>
    public bool HasWeatherCondition { get; }

    /// <summary>
    /// Gets a value indicating whether the condition depends on the runways in use.
    /// </summary>
    public bool HasRunwayCondition { get; }

    /// <summary>
    /// Parses a condition.
    /// </summary>
    /// <param name="text">The condition text.</param>
    /// <param name="expression">The parsed condition, when parsing succeeds.</param>
    /// <param name="error">A message describing the first problem, when parsing fails.</param>
    /// <returns>True if the text is a valid condition.</returns>
    public static bool TryParse(string? text, out ConditionExpression? expression, out string? error)
    {
        expression = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter a condition, e.g. EPKK_E_APP AND dep:07 AND ceiling < 200ft.";
            return false;
        }

        try
        {
            var parser = new Parser(Tokenize(text));
            var root = parser.ParseAll();
            expression = new ConditionExpression(root, parser);
            error = null;
            return true;
        }
        catch (FormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Converts a station condition and callsign list from before conditions were written as text.
    /// </summary>
    /// <param name="condition">The old condition.</param>
    /// <param name="callsigns">The old callsign list, separated by commas, semicolons or spaces.</param>
    /// <returns>The equivalent condition, e.g. <c>NOT (EPWA_DEL OR EPWA_APP)</c>, or null if there was none.</returns>
    public static string? FromLegacy(StationCondition condition, string? callsigns)
    {
        var list = (callsigns ?? string.Empty).Split([',', ';', ' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (condition == StationCondition.Always || list.Length == 0)
        {
            return null;
        }

        var any = string.Join(" OR ", list);
        return condition == StationCondition.WhenOnline ? any
            : list.Length == 1 ? $"NOT {any}"
            : $"NOT ({any})";
    }

    /// <summary>
    /// Joins the separate controller and weather conditions of an old profile into one condition, which is met when
    /// both are.
    /// </summary>
    /// <param name="online">The old controller condition, or null.</param>
    /// <param name="weather">The old weather condition, or null.</param>
    /// <returns>The combined condition, or null if there was none.</returns>
    public static string? Combine(string? online, string? weather)
    {
        online = string.IsNullOrWhiteSpace(online) ? null : online.Trim();
        weather = string.IsNullOrWhiteSpace(weather) ? null : weather.Trim();
        return online != null && weather != null ? $"({online}) AND ({weather})" : online ?? weather;
    }

    /// <summary>
    /// Evaluates the condition.
    /// </summary>
    /// <param name="context">What the condition can look at.</param>
    /// <returns>True if the condition is met.</returns>
    public bool Evaluate(ConditionContext context)
    {
        return _root(context) ?? false;
    }

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c is ',' or ';')
            {
                tokens.Add(new Token(TokenKind.Comma, c.ToString(), i++));
            }
            else if (c == '(')
            {
                tokens.Add(new Token(TokenKind.Open, "(", i++));
            }
            else if (c == ')')
            {
                tokens.Add(new Token(TokenKind.Close, ")", i++));
            }
            else if (c is '<' or '>' or '=')
            {
                var start = i++;
                if (i < text.Length && text[i] == '=')
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Operator, text[start..i], start));
            }
            else if (c == '"')
            {
                var start = i++;
                var close = text.IndexOf('"', i);
                if (close < 0)
                {
                    throw new FormatException($"Missing closing quote for the text at position {start + 1}.");
                }

                tokens.Add(new Token(TokenKind.Quoted, text[i..close], start));
                i = close + 1;
            }
            else if (char.IsDigit(c) && s_timeRange.Match(text, i) is { Success: true } time)
            {
                var token = new Token(TokenKind.Time, time.Value, i, ToMinutes(time.Groups[1].Value, i), ToMinutes(time.Groups[2].Value, i));
                if (token.Start == token.End)
                {
                    throw new FormatException(
                        $"The start and end of {time.Value.Trim()} are the same (position {i + 1}).");
                }

                tokens.Add(token);
                i += time.Length;
            }
            else
            {
                var start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) &&
                       text[i] is not (',' or ';' or '(' or ')' or '<' or '>' or '=' or '"'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Word, text[start..i], start));
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, text.Length));
        return tokens;
    }

    private static int ToMinutes(string hhmm, int position)
    {
        var hours = int.Parse(hhmm[..2], CultureInfo.InvariantCulture);
        var minutes = int.Parse(hhmm[2..], CultureInfo.InvariantCulture);

        // 2400 is allowed as the end of a range that runs to midnight.
        if (minutes > 59 || hours > 24 || (hours == 24 && minutes != 0))
        {
            throw new FormatException($"{hhmm} is not a valid time; use 0000 to 2359 (position {position + 1}).");
        }

        return (hours * 60) + minutes;
    }

    private static Matcher Wildcard(string pattern, Func<ConditionContext, IEnumerable<string>?> source)
    {
        var regex = new Regex(
            "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return context => (source(context) ?? []).Any(regex.IsMatch);
    }

    private static Matcher Callsign(string pattern)
    {
        return Wildcard(pattern, c => c.OnlineCallsigns);
    }

    private static Matcher Runway(string pattern, RunwayKind kind)
    {
        var arrival = Wildcard(pattern, c => c.Runways?.Arrival);
        var departure = Wildcard(pattern, c => c.Runways?.Departure);
        return context => (kind != RunwayKind.Arrival && departure(context) == true) ||
                          (kind != RunwayKind.Departure && arrival(context) == true);
    }

    private static Matcher TimeRange(int start, int end)
    {
        return context =>
        {
            var now = context.UtcNow ?? DateTime.UtcNow;
            var minutes = (now.Hour * 60) + now.Minute;
            return start < end ? minutes >= start && minutes < end : minutes >= start || minutes < end;
        };
    }

    private static Matcher Weather(WeatherRule rule)
    {
        // Nothing can be said about the weather without a METAR, not even that a value is missing.
        return context => context.Metar == null || context.AtisFormat == null
            ? null
            : WeatherRuleEvaluator.IsMet(rule, context.Metar, context.AtisFormat);
    }

    private static Matcher And(Matcher left, Matcher right)
    {
        return context =>
        {
            var l = left(context);
            if (l == false)
            {
                return false;
            }

            var r = right(context);
            return r == false ? false : l == null || r == null ? null : true;
        };
    }

    private static Matcher Or(Matcher left, Matcher right)
    {
        return context =>
        {
            var l = left(context);
            if (l == true)
            {
                return true;
            }

            var r = right(context);
            return r == true ? true : l == null || r == null ? null : false;
        };
    }

    private static Matcher Not(Matcher inner)
    {
        return context => !inner(context);
    }

    private enum RunwayKind
    {
        Any,
        Arrival,
        Departure,
    }

    private readonly record struct Token(TokenKind Kind, string Text, int Position, int Start = 0, int End = 0);

    private sealed class Parser(List<Token> tokens)
    {
        private int _position;

        // True when the last term was a callsign, runway or time, which can be listed without AND or OR.
        private bool _lastWasListable;

        public bool HasTime { get; private set; }

        public bool HasCallsign { get; private set; }

        public bool HasWeather { get; private set; }

        public bool HasRunway { get; private set; }

        private Token Current => tokens[_position];

        public Matcher ParseAll()
        {
            var node = ParseList();
            if (Current.Kind != TokenKind.End)
            {
                throw Error("There is a ')' without a matching '('.", Current);
            }

            return node;
        }

        private static FormatException Error(string message, Token at)
        {
            return new FormatException($"{message} (position {at.Position + 1})");
        }

        private static bool IsKeyword(Token token, string keyword)
        {
            return token.Kind == TokenKind.Word && token.Text.Equals(keyword, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReservedWord(Token token)
        {
            return IsKeyword(token, "and") || IsKeyword(token, "or") || IsKeyword(token, "not");
        }

        private static WeatherOperator? ToNumericOperator(Token token)
        {
            return token.Kind == TokenKind.Operator
                ? token.Text switch
                {
                    "<" => WeatherOperator.Below,
                    "<=" => WeatherOperator.AtOrBelow,
                    ">" => WeatherOperator.Above,
                    ">=" => WeatherOperator.AtOrAbove,
                    "=" or "==" => WeatherOperator.EqualTo,
                    _ => null,
                }
                : null;
        }

        private static double ParseNumber(string text, Token token)
        {
            var slash = text.IndexOf('/', StringComparison.Ordinal);
            return slash < 0
                ? double.Parse(text, CultureInfo.InvariantCulture)
                : ParseFraction(text[..slash], text[(slash + 1)..], token) * (text[0] == '-' ? -1 : 1);
        }

        private static double ParseFraction(string top, string bottom, Token token)
        {
            var denominator = double.Parse(bottom, CultureInfo.InvariantCulture);
            if (denominator == 0)
            {
                throw Error("A fraction cannot be divided by zero.", token);
            }

            return Math.Abs(double.Parse(top, CultureInfo.InvariantCulture)) / denominator;
        }

        // A word is a weather term when it names a weather field, or when a comparison follows it, which makes a
        // misspelled field an error about the field instead of a callsign followed by a stray comparison.
        private bool IsWeatherTerm(int index)
        {
            var token = tokens[index];
            return token.Kind == TokenKind.Word && !IsReservedWord(token) &&
                   (WeatherExpression.TryGetField(token.Text, out _) || tokens[index + 1].Kind == TokenKind.Operator);
        }

        private bool StartsListedTerm(int index)
        {
            var token = tokens[index];
            return token.Kind is TokenKind.Time or TokenKind.Open ||
                   (token.Kind == TokenKind.Word && !IsReservedWord(token) && !IsWeatherTerm(index));
        }

        private Matcher ParseList()
        {
            SkipCommas();
            var node = ParseOr();
            while (true)
            {
                var sawComma = SkipCommas();
                if (Current.Kind is TokenKind.End or TokenKind.Close)
                {
                    return node;
                }

                if (!sawComma && !(_lastWasListable && StartsListedTerm(_position)))
                {
                    throw Error(
                        Current.Kind == TokenKind.Operator
                            ? $"Unexpected '{Current.Text}'."
                            : $"Expected AND or OR before '{Current.Text}'.",
                        Current);
                }

                node = Or(node, ParseOr());
            }
        }

        private bool SkipCommas()
        {
            var skipped = false;
            while (Current.Kind == TokenKind.Comma)
            {
                _position++;
                skipped = true;
            }

            return skipped;
        }

        private Matcher ParseOr()
        {
            var node = ParseAnd();
            while (IsKeyword(Current, "or"))
            {
                _position++;
                node = Or(node, ParseAnd());
            }

            return node;
        }

        private Matcher ParseAnd()
        {
            var node = ParseNot();
            while (true)
            {
                if (IsKeyword(Current, "and"))
                {
                    _position++;
                    node = And(node, ParseNot());
                }
                else if (IsKeyword(Current, "not") && _lastWasListable)
                {
                    node = And(node, ParseNot());
                }
                else
                {
                    return node;
                }
            }
        }

        private Matcher ParseNot()
        {
            if (IsKeyword(Current, "not"))
            {
                _position++;
                return Not(ParseNot());
            }

            return ParsePrimary();
        }

        private Matcher ParsePrimary()
        {
            var token = Current;
            if (token.Kind == TokenKind.Open)
            {
                _position++;
                var inner = ParseList();
                if (Current.Kind != TokenKind.Close)
                {
                    throw Error("Missing a closing ')'.", token);
                }

                _position++;
                return inner;
            }

            if (token.Kind == TokenKind.Time)
            {
                _position++;
                HasTime = true;
                _lastWasListable = true;
                return TimeRange(token.Start, token.End);
            }

            if (token.Kind == TokenKind.Word && s_singleTime.IsMatch(token.Text))
            {
                throw Error($"Write a time as a range, like {token.Text}-1400z.", token);
            }

            if (token.Kind != TokenKind.Word || IsKeyword(token, "and") || IsKeyword(token, "or"))
            {
                throw Error(
                    token.Kind == TokenKind.End
                        ? "The condition ends too soon; expected a condition."
                        : $"Expected a condition, not '{token.Text}'.",
                    token);
            }

            if (IsWeatherTerm(_position))
            {
                var term = ParseWeather();
                HasWeather = true;
                _lastWasListable = false;
                return term;
            }

            _position++;
            _lastWasListable = true;
            var colon = token.Text.IndexOf(':');
            if (colon < 0)
            {
                HasCallsign = true;
                return Callsign(token.Text);
            }

            HasRunway = true;
            return ParseRunway(token, colon);
        }

        private Matcher ParseRunway(Token token, int colon)
        {
            var prefix = token.Text[..colon];
            var kind = prefix.ToUpperInvariant() switch
            {
                "DEP" or "DEPARTURE" => RunwayKind.Departure,
                "ARR" or "ARRIVAL" => RunwayKind.Arrival,
                "RWY" or "RUNWAY" => RunwayKind.Any,
                _ => throw Error($"Use dep:, arr: or rwy: before a runway, not '{prefix}:'.", token),
            };

            var runway = token.Text[(colon + 1)..];
            if (runway.Length == 0)
            {
                throw Error($"Enter a runway after '{prefix}:', e.g. {prefix}:07L.", token);
            }

            return Runway(runway, kind);
        }

        private Matcher ParseWeather()
        {
            var fieldToken = Current;
            if (!WeatherExpression.TryGetField(fieldToken.Text, out var field, out var trend))
            {
                throw Error($"Unknown field '{fieldToken.Text}'. Fields: {WeatherExpression.FieldList}.", fieldToken);
            }

            _position++;
            var rule = WeatherFieldInfo.GetKind(field) switch
            {
                WeatherFieldKind.Flag => ParseFlag(field, trend, fieldToken),
                WeatherFieldKind.Text => ParseText(field, trend, fieldToken),
                _ => ParseNumeric(field, trend, fieldToken),
            };
            return Weather(rule);
        }

        private WeatherRule ParseFlag(WeatherField field, TrendScope trend, Token fieldToken)
        {
            if (Current.Kind == TokenKind.Operator)
            {
                throw Error($"'{fieldToken.Text}' is either reported or not; use NOT {fieldToken.Text} for the opposite.", Current);
            }

            return new WeatherRule { Field = field, Trend = trend, Operator = WeatherOperator.Contains };
        }

        private WeatherRule ParseText(WeatherField field, TrendScope trend, Token fieldToken)
        {
            var op = Current;
            WeatherOperator? oper = null;
            if (op.Kind == TokenKind.Word)
            {
                if (op.Text.Equals("includes", StringComparison.OrdinalIgnoreCase) ||
                    op.Text.Equals("include", StringComparison.OrdinalIgnoreCase) ||
                    op.Text.Equals("contains", StringComparison.OrdinalIgnoreCase))
                {
                    oper = WeatherOperator.Contains;
                }
                else if (op.Text.Equals("excludes", StringComparison.OrdinalIgnoreCase) ||
                         op.Text.Equals("exclude", StringComparison.OrdinalIgnoreCase))
                {
                    oper = WeatherOperator.NotContains;
                }
            }

            if (oper == null)
            {
                throw Error($"Expected INCLUDES or EXCLUDES after '{fieldToken.Text}', e.g. {fieldToken.Text} INCLUDES TS.", op);
            }

            _position++;
            var value = Current;
            if (value.Kind is not (TokenKind.Word or TokenKind.Quoted) || (value.Kind == TokenKind.Word && IsReservedWord(value)))
            {
                throw Error($"Expected the codes to look for after '{op.Text}', e.g. TS or \"TS FZRA\".", value);
            }

            if (string.IsNullOrWhiteSpace(value.Text))
            {
                throw Error("Enter at least one code between the quotes.", value);
            }

            _position++;
            var text = value.Text;

            // Codes written with nothing around the commas, e.g. TS,FZRA, are one list.
            if (value.Kind == TokenKind.Word)
            {
                var end = value.Position + value.Text.Length;
                while (Current is { Kind: TokenKind.Comma } comma && comma.Position == end &&
                       tokens[_position + 1] is { Kind: TokenKind.Word } next && next.Position == end + 1 &&
                       !IsReservedWord(next))
                {
                    text += comma.Text + next.Text;
                    end = next.Position + next.Text.Length;
                    _position += 2;
                }
            }

            return new WeatherRule { Field = field, Trend = trend, Operator = oper.Value, Text = text.ToUpperInvariant() };
        }

        private WeatherRule ParseNumeric(WeatherField field, TrendScope trend, Token fieldToken)
        {
            var opToken = Current;
            var oper = ToNumericOperator(opToken);
            if (oper == null)
            {
                throw Error($"Expected a comparison (<, <=, >, >=, =) after '{fieldToken.Text}'.", opToken);
            }

            _position++;
            var numberToken = Current;
            var match = numberToken.Kind == TokenKind.Word ? WeatherExpression.NumberPattern().Match(numberToken.Text) : Match.Empty;
            if (!match.Success)
            {
                throw Error($"Expected a number after '{opToken.Text}'.", numberToken);
            }

            _position++;
            var value = ParseNumber(match.Groups[1].Value, numberToken);
            var allowed = WeatherFieldInfo.GetUnits(field);
            var unitText = match.Groups[2].Value;
            var unitToken = numberToken;

            // A whole number followed by a fraction is a mixed number, e.g. "1 1/2sm".
            if (unitText.Length == 0 && !match.Groups[1].Value.Contains('/') && !match.Groups[1].Value.Contains('.') &&
                Current.Kind == TokenKind.Word && WeatherExpression.FractionPattern().Match(Current.Text) is { Success: true } fraction)
            {
                value += Math.Sign(value == 0 ? 1 : value) * ParseFraction(fraction.Groups[1].Value, fraction.Groups[2].Value, Current);
                unitText = fraction.Groups[3].Value;
                unitToken = Current;
                _position++;
            }

            // The unit can also follow after a space, e.g. "200 ft".
            if (unitText.Length == 0 && Current.Kind == TokenKind.Word && WeatherExpression.s_units.ContainsKey(Current.Text))
            {
                unitToken = Current;
                unitText = Current.Text;
                _position++;
            }

            var unit = allowed[0];
            if (unitText.Length > 0)
            {
                if (!WeatherExpression.s_units.TryGetValue(unitText, out unit) || !allowed.Contains(unit))
                {
                    throw Error(
                        $"'{unitText}' is not a unit for {fieldToken.Text}. Use {string.Join(", ", allowed.Select(WeatherExpression.GetUnitName))}.",
                        unitToken);
                }
            }

            return new WeatherRule { Field = field, Trend = trend, Operator = oper.Value, Value = value, Unit = unit };
        }
    }
}
