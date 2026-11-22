// <copyright file="RunwayExpression.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// A runway condition written as text, e.g. <c>dep:07</c>, <c>arr:25 OR arr:25*</c> or <c>NOT dep:07R</c>.
/// </summary>
/// <remarks>
/// Grammar (keywords are not case-sensitive, and runways are matched without regard to case):
/// <code>
/// list    := or ( [,;] or )*
/// or      := and ( OR and )*
/// and     := not ( [AND] not | NOT not )*
/// not     := NOT not | primary
/// primary := '(' list ')' | [dep: | arr:] runway
/// </code>
/// Terms with nothing between them are an implicit OR, so a plain list of runways means any of them. A NOT straight
/// after a term means AND NOT. A runway is true when it is one of the preset's runways: <c>dep:</c> only looks at
/// the departure runways, <c>arr:</c> only at the arrival runways, and with neither it looks at both. A * matches any
/// characters, so <c>dep:07*</c> matches 07L and 07R.
/// </remarks>
public sealed class RunwayExpression
{
    private readonly Matcher _root;

    private RunwayExpression(Matcher root)
    {
        _root = root;
    }

    private delegate bool Matcher(ActiveRunways active);

    private enum RunwayKind
    {
        Any,
        Arrival,
        Departure,
    }

    private enum TokenKind
    {
        Word,
        Open,
        Close,
        End,
    }

    /// <summary>
    /// Parses a runway configuration condition.
    /// </summary>
    /// <param name="text">The condition text.</param>
    /// <param name="expression">The parsed condition, when parsing succeeds.</param>
    /// <param name="error">A message describing the first problem, when parsing fails.</param>
    /// <returns>True if the text is a valid condition.</returns>
    public static bool TryParse(string? text, out RunwayExpression? expression, out string? error)
    {
        expression = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter at least one runway.";
            return false;
        }

        try
        {
            var parser = new Parser(Tokenize(text));
            var root = parser.ParseAll();
            expression = new RunwayExpression(root);
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
    /// Evaluates the condition.
    /// </summary>
    /// <param name="active">The active runways.</param>
    /// <returns>True if the condition is met.</returns>
    public bool Evaluate(ActiveRunways active)
    {
        return _root(active);
    }

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (char.IsWhiteSpace(c) || c is ',' or ';')
            {
                i++;
            }
            else if (c == '(')
            {
                tokens.Add(new Token(TokenKind.Open, "(", i++, false));
            }
            else if (c == ')')
            {
                tokens.Add(new Token(TokenKind.Close, ")", i++, false));
            }
            else if (c == '"')
            {
                var close = text.IndexOf('"', i + 1);
                if (close < 0)
                {
                    throw new FormatException($"Missing a closing quote (position {i + 1}).");
                }

                var name = text[(i + 1)..close].Trim();
                if (name.Length == 0)
                {
                    throw new FormatException($"A quoted name is empty (position {i + 1}).");
                }

                tokens.Add(new Token(TokenKind.Word, name, i, true));
                i = close + 1;
            }
            else
            {
                var start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] is not (',' or ';' or '(' or ')' or '"'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Word, text[start..i], start, false));
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, text.Length, false));
        return tokens;
    }

    private static Matcher Runway(string text)
    {
        var kind = RunwayKind.Any;
        var pattern = text;
        var colon = text.IndexOf(':');
        if (colon >= 0)
        {
            var prefix = text[..colon];
            if (prefix.Equals("dep", StringComparison.OrdinalIgnoreCase) ||
                prefix.Equals("departure", StringComparison.OrdinalIgnoreCase))
            {
                kind = RunwayKind.Departure;
            }
            else if (prefix.Equals("arr", StringComparison.OrdinalIgnoreCase) ||
                     prefix.Equals("arrival", StringComparison.OrdinalIgnoreCase))
            {
                kind = RunwayKind.Arrival;
            }
            else
            {
                throw new FormatException($"Use dep: or arr: before a runway, not '{prefix}:'.");
            }

            pattern = text[(colon + 1)..];
            if (pattern.Length == 0)
            {
                throw new FormatException($"Enter a runway after '{prefix}:'.");
            }
        }

        var regex = new Regex(
            "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return active => (kind != RunwayKind.Arrival && active.Departure.Any(regex.IsMatch)) ||
                         (kind != RunwayKind.Departure && active.Arrival.Any(regex.IsMatch));
    }

    private static Matcher And(Matcher left, Matcher right)
    {
        return active => left(active) && right(active);
    }

    private static Matcher Or(Matcher left, Matcher right)
    {
        return active => left(active) || right(active);
    }

    private static Matcher Not(Matcher inner)
    {
        return active => !inner(active);
    }

    private readonly record struct Token(TokenKind Kind, string Text, int Position, bool Quoted);

    private sealed class Parser(List<Token> tokens)
    {
        private int _position;

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
            return token.Kind == TokenKind.Word && !token.Quoted &&
                   token.Text.Equals(keyword, StringComparison.OrdinalIgnoreCase);
        }

        private Matcher ParseList()
        {
            var node = ParseOr();
            while (Current.Kind is not (TokenKind.End or TokenKind.Close))
            {
                node = Or(node, ParseOr());
            }

            return node;
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
                else if (IsKeyword(Current, "not"))
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

            if (token.Kind != TokenKind.Word || IsKeyword(token, "and") || IsKeyword(token, "or"))
            {
                throw Error(
                    token.Kind == TokenKind.End
                        ? "The condition ends too soon; expected a runway."
                        : $"Expected a runway, not '{token.Text}'.",
                    token);
            }

            _position++;
            try
            {
                return Runway(token.Text);
            }
            catch (FormatException ex)
            {
                throw Error(ex.Message, token);
            }
        }
    }
}

/// <summary>
/// The runways in use for a preset.
/// </summary>
/// <param name="Arrival">The arrival runways, e.g. <c>07L</c>.</param>
/// <param name="Departure">The departure runways.</param>
public sealed record ActiveRunways(IReadOnlyCollection<string> Arrival, IReadOnlyCollection<string> Departure)
{
    /// <summary>
    /// Gets an instance with no runways.
    /// </summary>
    public static ActiveRunways None { get; } = new([], []);

    /// <summary>
    /// Reads the runways from a preset.
    /// </summary>
    /// <param name="preset">The preset, or null.</param>
    /// <returns>The preset's runways.</returns>
    public static ActiveRunways From(Vatsim.Vatis.Profiles.Models.AtisPreset? preset)
    {
        return preset == null ? None : new ActiveRunways(Split(preset.ArrivalRunways), Split(preset.DepartureRunways));
    }

    /// <summary>
    /// Splits a list of runways separated by commas, semicolons or spaces.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The runways.</returns>
    public static string[] Split(string? text)
    {
        return (text ?? string.Empty).Split([',', ';', ' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }
}
