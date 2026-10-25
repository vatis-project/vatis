// <copyright file="WeatherExpression.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// A weather condition written as text, e.g. <c>ceiling &lt; 200ft AND (rvr &lt; 550m OR visibility &lt; 100m)</c>.
/// </summary>
/// <remarks>
/// Grammar (keywords are not case-sensitive, and AND binds tighter than OR):
/// <code>
/// or        := and ( OR and )*
/// and       := not ( AND not )*
/// not       := NOT not | primary
/// primary   := '(' or ')' | condition
/// condition := numeric-field ( '&lt;' | '&lt;=' | '&gt;' | '&gt;=' | '=' ) number [unit]
///            | text-field ( INCLUDES | EXCLUDES ) codes
///            | flag-field
/// </code>
/// </remarks>
public sealed partial class WeatherExpression
{
    private static readonly Dictionary<string, WeatherField> s_fields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ceiling"] = WeatherField.Ceiling,
        ["cloudbase"] = WeatherField.CloudBase,
        ["rvr"] = WeatherField.Rvr,
        ["visibility"] = WeatherField.Visibility,
        ["vis"] = WeatherField.Visibility,
        ["windspeed"] = WeatherField.WindSpeed,
        ["wind"] = WeatherField.WindSpeed,
        ["gust"] = WeatherField.WindGust,
        ["windgust"] = WeatherField.WindGust,
        ["winddirection"] = WeatherField.WindDirection,
        ["winddir"] = WeatherField.WindDirection,
        ["temperature"] = WeatherField.Temperature,
        ["temp"] = WeatherField.Temperature,
        ["dewpoint"] = WeatherField.Dewpoint,
        ["dew"] = WeatherField.Dewpoint,
        ["pressure"] = WeatherField.Pressure,
        ["qnh"] = WeatherField.Pressure,
        ["altimeter"] = WeatherField.Pressure,
        ["depth"] = WeatherField.RunwayDepth,
        ["runwaydepth"] = WeatherField.RunwayDepth,
        ["weather"] = WeatherField.PresentWeather,
        ["wx"] = WeatherField.PresentWeather,
        ["recentweather"] = WeatherField.RecentWeather,
        ["recentwx"] = WeatherField.RecentWeather,
        ["trendtype"] = WeatherField.TrendType,
        ["cloudtype"] = WeatherField.CloudType,
        ["windshear"] = WeatherField.Windshear,
        ["ws"] = WeatherField.Windshear,
        ["cavok"] = WeatherField.Cavok,
    };

    private static readonly (string Prefix, TrendScope Scope)[] s_trendPrefixes =
    [
        ("trend", TrendScope.Any),
        ("tempo", TrendScope.Tempo),
        ("becmg", TrendScope.Becoming),
    ];

    // The values a trend forecast can carry, which can be read by prefixing the field with "trend", "tempo" or
    // "becmg".
    private static readonly HashSet<WeatherField> s_trendFields =
    [
        WeatherField.Ceiling,
        WeatherField.CloudBase,
        WeatherField.Visibility,
        WeatherField.WindSpeed,
        WeatherField.WindGust,
        WeatherField.WindDirection,
        WeatherField.PresentWeather,
        WeatherField.CloudType,
        WeatherField.Cavok,
    ];

    private static readonly Dictionary<string, WeatherUnit> s_units = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ft"] = WeatherUnit.Feet,
        ["feet"] = WeatherUnit.Feet,
        ["m"] = WeatherUnit.Meters,
        ["meters"] = WeatherUnit.Meters,
        ["metres"] = WeatherUnit.Meters,
        ["km"] = WeatherUnit.Kilometers,
        ["sm"] = WeatherUnit.StatuteMiles,
        ["mi"] = WeatherUnit.StatuteMiles,
        ["miles"] = WeatherUnit.StatuteMiles,
        ["kt"] = WeatherUnit.Knots,
        ["kts"] = WeatherUnit.Knots,
        ["knots"] = WeatherUnit.Knots,
        ["kmh"] = WeatherUnit.KilometersPerHour,
        ["km/h"] = WeatherUnit.KilometersPerHour,
        ["kph"] = WeatherUnit.KilometersPerHour,
        ["mps"] = WeatherUnit.MetersPerSecond,
        ["m/s"] = WeatherUnit.MetersPerSecond,
        ["deg"] = WeatherUnit.Degrees,
        ["degrees"] = WeatherUnit.Degrees,
        ["c"] = WeatherUnit.Celsius,
        ["f"] = WeatherUnit.Fahrenheit,
        ["hpa"] = WeatherUnit.HectoPascals,
        ["mb"] = WeatherUnit.HectoPascals,
        ["inhg"] = WeatherUnit.InchesOfMercury,
        ["mm"] = WeatherUnit.Millimeters,
    };

    private readonly ExpressionNode _root;

    private WeatherExpression(ExpressionNode root)
    {
        _root = root;
    }

    private enum TokenKind
    {
        Word,
        Quoted,
        Operator,
        Open,
        Close,
        End,
    }

    /// <summary>
    /// Gets a short description of the syntax, for tooltips and error messages.
    /// </summary>
    public static string FieldList =>
        "ceiling, cloudbase, rvr, visibility, windspeed, gust, winddir, temperature, dewpoint, qnh, depth, " +
        "weather, recentweather, trendtype, cloudtype, windshear, cavok. Prefix a field with trend, tempo or becmg (e.g. tempoceiling) to read the trend";

    /// <summary>
    /// Looks up a field by name or alias, ignoring case.
    /// </summary>
    /// <param name="name">The name as typed.</param>
    /// <param name="field">The field, when found.</param>
    /// <returns>True if the name is a known field.</returns>
    public static bool TryGetField(string name, out WeatherField field)
    {
        return TryGetField(name, out field, out _);
    }

    /// <summary>
    /// Looks up a field by name or alias, ignoring case. A trend field is a field the trend can carry with "trend",
    /// "tempo" or "becmg" in front, e.g. trendceiling or tempoweather.
    /// </summary>
    /// <param name="name">The name as typed.</param>
    /// <param name="field">The field, when found.</param>
    /// <param name="trend">Which trend groups the field is read from, or none for the METAR.</param>
    /// <returns>True if the name is a known field.</returns>
    public static bool TryGetField(string name, out WeatherField field, out TrendScope trend)
    {
        trend = TrendScope.None;
        if (s_fields.TryGetValue(name, out field))
        {
            return true;
        }

        foreach (var (prefix, scope) in s_trendPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                s_fields.TryGetValue(name[prefix.Length..], out field) && s_trendFields.Contains(field))
            {
                trend = scope;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the name used for a unit when writing a condition, e.g. <c>ft</c> or <c>kt</c>.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The unit as typed in a condition.</returns>
    public static string GetUnitName(WeatherUnit unit)
    {
        return s_units.First(x => x.Value == unit).Key;
    }

    /// <summary>
    /// Parses a weather condition.
    /// </summary>
    /// <param name="text">The condition text.</param>
    /// <param name="expression">The parsed condition, when parsing succeeds.</param>
    /// <param name="error">A message describing the first problem, when parsing fails.</param>
    /// <returns>True if the text is a valid condition.</returns>
    public static bool TryParse(string? text, out WeatherExpression? expression, out string? error)
    {
        expression = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter a condition, e.g. ceiling < 200ft AND rvr < 550m.";
            return false;
        }

        try
        {
            var parser = new Parser(Tokenize(text));
            expression = new WeatherExpression(parser.ParseAll());
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
    /// Evaluates the condition. Comparisons are not met when the METAR does not report the value they need.
    /// </summary>
    /// <param name="metar">The decoded METAR, or null when none is available.</param>
    /// <param name="atisFormat">The station format, which decides which cloud layers count as a ceiling.</param>
    /// <returns>True if the condition is met.</returns>
    public bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat)
    {
        // Without a METAR nothing can be said about the weather, so no condition is met, NOT included.
        return metar != null && _root.Evaluate(metar, atisFormat);
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
            else
            {
                var start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] is not ('(' or ')' or '<' or '>' or '=' or '"'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Word, text[start..i], start));
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, text.Length));
        return tokens;
    }

    // A number is a whole or decimal number, or a fraction such as 1/2 as in 1/2SM.
    [GeneratedRegex(@"^([+-]?\d+(?:\.\d+)?(?:/\d+)?)(.*)$")]
    private static partial Regex NumberPattern();

    // The fraction of a mixed number, e.g. the 1/2SM in "1 1/2SM".
    [GeneratedRegex(@"^(\d+)/(\d+)(.*)$")]
    private static partial Regex FractionPattern();

    private readonly record struct Token(TokenKind Kind, string Text, int Position);

    private sealed class Parser(List<Token> tokens)
    {
        private int _position;

        private Token Current => tokens[_position];

        public ExpressionNode ParseAll()
        {
            var node = ParseOr();
            if (Current.Kind != TokenKind.End)
            {
                throw Error(
                    Current.Kind == TokenKind.Close
                        ? "There is a ')' without a matching '('."
                        : $"Expected AND or OR before '{Current.Text}'.",
                    Current);
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

        private ExpressionNode ParseOr()
        {
            var node = ParseAnd();
            while (IsKeyword(Current, "or"))
            {
                _position++;
                node = new OrNode(node, ParseAnd());
            }

            return node;
        }

        private ExpressionNode ParseAnd()
        {
            var node = ParseNot();
            while (IsKeyword(Current, "and"))
            {
                _position++;
                node = new AndNode(node, ParseNot());
            }

            return node;
        }

        private ExpressionNode ParseNot()
        {
            if (IsKeyword(Current, "not"))
            {
                _position++;
                return new NotNode(ParseNot());
            }

            return ParsePrimary();
        }

        private ExpressionNode ParsePrimary()
        {
            var token = Current;
            if (token.Kind == TokenKind.Open)
            {
                _position++;
                var inner = ParseOr();
                if (Current.Kind != TokenKind.Close)
                {
                    throw Error("Missing a closing ')'.", token);
                }

                _position++;
                return inner;
            }

            if (token.Kind != TokenKind.Word || IsKeyword(token, "and") || IsKeyword(token, "or"))
            {
                throw Error(
                    token.Kind == TokenKind.End ? "The condition ends too soon; expected a field name." : $"Expected a field name, not '{token.Text}'.",
                    token);
            }

            return ParseCondition();
        }

        private ExpressionNode ParseCondition()
        {
            var fieldToken = Current;
            if (!TryGetField(fieldToken.Text, out var field, out var trend))
            {
                throw Error($"Unknown field '{fieldToken.Text}'. Fields: {FieldList}.", fieldToken);
            }

            _position++;
            return WeatherFieldInfo.GetKind(field) switch
            {
                WeatherFieldKind.Flag => ParseFlag(field, trend, fieldToken),
                WeatherFieldKind.Text => ParseText(field, trend, fieldToken),
                _ => ParseNumeric(field, trend, fieldToken),
            };
        }

        private ExpressionNode ParseFlag(WeatherField field, TrendScope trend, Token fieldToken)
        {
            if (Current.Kind == TokenKind.Operator)
            {
                throw Error($"'{fieldToken.Text}' is either reported or not; use NOT {fieldToken.Text} for the opposite.", Current);
            }

            return new RuleNode(new WeatherRule { Field = field, Trend = trend, Operator = WeatherOperator.Contains });
        }

        private ExpressionNode ParseText(WeatherField field, TrendScope trend, Token fieldToken)
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
            return new RuleNode(new WeatherRule { Field = field, Trend = trend, Operator = oper.Value, Text = value.Text.ToUpperInvariant() });
        }

        private ExpressionNode ParseNumeric(WeatherField field, TrendScope trend, Token fieldToken)
        {
            var opToken = Current;
            var oper = ToNumericOperator(opToken);
            if (oper == null)
            {
                throw Error($"Expected a comparison (<, <=, >, >=, =) after '{fieldToken.Text}'.", opToken);
            }

            _position++;
            var numberToken = Current;
            var match = numberToken.Kind == TokenKind.Word ? NumberPattern().Match(numberToken.Text) : Match.Empty;
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
                Current.Kind == TokenKind.Word && FractionPattern().Match(Current.Text) is { Success: true } fraction)
            {
                value += Math.Sign(value == 0 ? 1 : value) * ParseFraction(fraction.Groups[1].Value, fraction.Groups[2].Value, Current);
                unitText = fraction.Groups[3].Value;
                unitToken = Current;
                _position++;
            }

            // The unit can also follow after a space, e.g. "200 ft".
            if (unitText.Length == 0 && Current.Kind == TokenKind.Word && s_units.ContainsKey(Current.Text))
            {
                unitToken = Current;
                unitText = Current.Text;
                _position++;
            }

            var unit = allowed[0];
            if (unitText.Length > 0)
            {
                if (!s_units.TryGetValue(unitText, out unit) || !allowed.Contains(unit))
                {
                    throw Error(
                        $"'{unitText}' is not a unit for {fieldToken.Text}. Use {string.Join(", ", allowed.Select(GetUnitName))}.",
                        unitToken);
                }
            }

            return new RuleNode(new WeatherRule { Field = field, Trend = trend, Operator = oper.Value, Value = value, Unit = unit });
        }
    }
}
