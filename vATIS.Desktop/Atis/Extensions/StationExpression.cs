// <copyright file="StationExpression.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// A station condition written as text, e.g. <c>LAS_E_GND NOT LAS_W_GND</c> or <c>EPWA_APP, EPWW_*_CTR</c>.
/// </summary>
/// <remarks>
/// Grammar (keywords are not case-sensitive, and * in a callsign matches any characters):
/// <code>
/// list    := or ( [,;] or )*
/// or      := and ( OR and )*
/// and     := not ( [AND] not | NOT not )*
/// not     := NOT not | primary
/// primary := '(' list ')' | callsign
/// </code>
/// Terms with nothing between them are an implicit OR, so a plain list of callsigns means any of them. A NOT straight
/// after a term means AND NOT. A callsign is true when any online controller matches it.
/// </remarks>
public sealed class StationExpression
{
    private readonly Node _root;

    private StationExpression(Node root)
    {
        _root = root;
    }

    private enum TokenKind
    {
        Word,
        Open,
        Close,
        End,
    }

    /// <summary>
    /// Parses a station condition.
    /// </summary>
    /// <param name="text">The condition text.</param>
    /// <param name="expression">The parsed condition, when parsing succeeds.</param>
    /// <param name="error">A message describing the first problem, when parsing fails.</param>
    /// <returns>True if the text is a valid condition.</returns>
    public static bool TryParse(string? text, out StationExpression? expression, out string? error)
    {
        expression = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter at least one callsign.";
            return false;
        }

        try
        {
            var parser = new Parser(Tokenize(text));
            expression = new StationExpression(parser.ParseAll());
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
    /// Evaluates the condition.
    /// </summary>
    /// <param name="onlineCallsigns">The online controller callsigns, or null when unknown. Unknown is treated as
    /// nobody online.</param>
    /// <returns>True if the condition is met.</returns>
    public bool Evaluate(IReadOnlyCollection<string>? onlineCallsigns)
    {
        return _root.Evaluate(onlineCallsigns ?? []);
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
                tokens.Add(new Token(TokenKind.Open, "(", i++));
            }
            else if (c == ')')
            {
                tokens.Add(new Token(TokenKind.Close, ")", i++));
            }
            else
            {
                var start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] is not (',' or ';' or '(' or ')'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Word, text[start..i], start));
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, text.Length));
        return tokens;
    }

    private readonly record struct Token(TokenKind Kind, string Text, int Position);

    private sealed class Parser(List<Token> tokens)
    {
        private int _position;

        private Token Current => tokens[_position];

        public Node ParseAll()
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

        private Node ParseList()
        {
            var node = ParseOr();
            while (Current.Kind is not (TokenKind.End or TokenKind.Close))
            {
                node = new OrNode(node, ParseOr());
            }

            return node;
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
            while (true)
            {
                if (IsKeyword(Current, "and"))
                {
                    _position++;
                    node = new AndNode(node, ParseNot());
                }
                else if (IsKeyword(Current, "not"))
                {
                    node = new AndNode(node, ParseNot());
                }
                else
                {
                    return node;
                }
            }
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
                        ? "The condition ends too soon; expected a callsign."
                        : $"Expected a callsign, not '{token.Text}'.",
                    token);
            }

            _position++;
            return new CallsignNode(token.Text);
        }
    }

    private abstract class Node
    {
        public abstract bool Evaluate(IReadOnlyCollection<string> online);
    }

    private sealed class CallsignNode(string pattern) : Node
    {
        private readonly Regex _regex = new(
            "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public override bool Evaluate(IReadOnlyCollection<string> online)
        {
            return online.Any(_regex.IsMatch);
        }
    }

    private sealed class AndNode(Node left, Node right) : Node
    {
        public override bool Evaluate(IReadOnlyCollection<string> online)
        {
            return left.Evaluate(online) && right.Evaluate(online);
        }
    }

    private sealed class OrNode(Node left, Node right) : Node
    {
        public override bool Evaluate(IReadOnlyCollection<string> online)
        {
            return left.Evaluate(online) || right.Evaluate(online);
        }
    }

    private sealed class NotNode(Node inner) : Node
    {
        public override bool Evaluate(IReadOnlyCollection<string> online)
        {
            return !inner.Evaluate(online);
        }
    }
}
