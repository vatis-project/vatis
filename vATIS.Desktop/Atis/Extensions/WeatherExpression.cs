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
        ["cloudtype"] = WeatherField.CloudType,
        ["windshear"] = WeatherField.Windshear,
        ["ws"] = WeatherField.Windshear,
        ["cavok"] = WeatherField.Cavok,
    };

    private static readonly Dictionary<string, WeatherUnit> s_units = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ft"] = WeatherUnit.Feet,
        ["feet"] = WeatherUnit.Feet,
        ["m"] = WeatherUnit.Meters,
        ["meters"] = WeatherUnit.Meters,
        ["metres"] = WeatherUnit.Meters,
        ["km"] = WeatherUnit.Kilometers,
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

    private readonly Node _root;

    private WeatherExpression(Node root)
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
        "weather, cloudtype, windshear, cavok";

    /// <summary>
    /// Looks up a field by name or alias, ignoring case.
    /// </summary>
    /// <param name="name">The name as typed.</param>
    /// <param name="field">The field, when found.</param>
    /// <returns>True if the name is a known field.</returns>
    public static bool TryGetField(string name, out WeatherField field)
    {
        return s_fields.TryGetValue(name, out field);
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

    [GeneratedRegex(@"^([+-]?\d+(?:\.\d+)?)(.*)$")]
    private static partial Regex NumberPattern();

    private readonly record struct Token(TokenKind Kind, string Text, int Position);

    private abstract class Node
    {
        public abstract bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat);
    }

    private sealed class RuleNode(WeatherRule rule) : Node
    {
        public override bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat)
        {
            return WeatherRuleEvaluator.IsMet(rule, metar, atisFormat);
        }
    }

    private sealed class NotNode(Node inner) : Node
    {
        public override bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat)
        {
            return !inner.Evaluate(metar, atisFormat);
        }
    }

    private sealed class AndNode(Node left, Node right) : Node
    {
        public override bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat)
        {
            return left.Evaluate(metar, atisFormat) && right.Evaluate(metar, atisFormat);
        }
    }

    private sealed class OrNode(Node left, Node right) : Node
    {
        public override bool Evaluate(DecodedMetar? metar, AtisFormat atisFormat)
        {
            return left.Evaluate(metar, atisFormat) || right.Evaluate(metar, atisFormat);
        }
    }

    private sealed class Parser(List<Token> tokens)
    {
        private int _position;

        private Token Current => tokens[_position];

        public Node ParseAll()
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

        private Node ParseOr()
        {
            var node = ParseAnd();
            while (IsKeyword(Current, "or"))
            {
                _position++;
                node = new OrNode(node, ParseAnd());
            }

            return node;
        }

        private Node ParseAnd()
        {
            var node = ParseNot();
            while (IsKeyword(Current, "and"))
            {
                _position++;
                node = new AndNode(node, ParseNot());
            }

            return node;
        }

        private Node ParseNot()
        {
            if (IsKeyword(Current, "not"))
            {
                _position++;
                return new NotNode(ParseNot());
            }

            return ParsePrimary();
        }

        private Node ParsePrimary()
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

        private Node ParseCondition()
        {
            var fieldToken = Current;
            if (!s_fields.TryGetValue(fieldToken.Text, out var field))
            {
                throw Error($"Unknown field '{fieldToken.Text}'. Fields: {FieldList}.", fieldToken);
            }

            _position++;
            return WeatherFieldInfo.GetKind(field) switch
            {
                WeatherFieldKind.Flag => ParseFlag(field, fieldToken),
                WeatherFieldKind.Text => ParseText(field, fieldToken),
                _ => ParseNumeric(field, fieldToken),
            };
        }

        private Node ParseFlag(WeatherField field, Token fieldToken)
        {
            if (Current.Kind == TokenKind.Operator)
            {
                throw Error($"'{fieldToken.Text}' is either reported or not; use NOT {fieldToken.Text} for the opposite.", Current);
            }

            return new RuleNode(new WeatherRule { Field = field, Operator = WeatherOperator.Contains });
        }

        private Node ParseText(WeatherField field, Token fieldToken)
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
            return new RuleNode(new WeatherRule { Field = field, Operator = oper.Value, Text = value.Text.ToUpperInvariant() });
        }

        private Node ParseNumeric(WeatherField field, Token fieldToken)
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
            var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var allowed = WeatherFieldInfo.GetUnits(field);
            var unitText = match.Groups[2].Value;
            var unitToken = numberToken;

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

            return new RuleNode(new WeatherRule { Field = field, Operator = oper.Value, Value = value, Unit = unit });
        }
    }
}
