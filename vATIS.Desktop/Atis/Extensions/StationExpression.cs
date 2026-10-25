// <copyright file="StationExpression.cs" company="Justin Shannon">
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
/// A station condition written as text, e.g. <c>LAS_E_GND NOT LAS_W_GND</c> or <c>EPWA_APP, EPWW_*_CTR</c>. It can
/// also test the time of day in Zulu, e.g. <c>LAX_TWR AND 2200z-0600z</c>.
/// </summary>
/// <remarks>
/// Grammar (keywords are not case-sensitive, and * in a callsign matches any characters):
/// <code>
/// list    := or ( [,;] or )*
/// or      := and ( OR and )*
/// and     := not ( [AND] not | NOT not )*
/// not     := NOT not | primary
/// primary := '(' list ')' | time-range | callsign
/// time-range := hhmm [z] '-' hhmm [z]
/// </code>
/// Terms with nothing between them are an implicit OR, so a plain list of callsigns means any of them. A NOT straight
/// after a term means AND NOT. A callsign is true when any online controller matches it. A time range is true from
/// its start up to, but not including, its end, and wraps past midnight when the end is before the start.
/// </remarks>
public sealed class StationExpression
{
    private delegate bool Matcher(IReadOnlyCollection<string> online, int minutes);

    private static readonly Regex s_timeRange = new(
        @"\G(\d{4})\s*[zZ]?\s*-\s*(\d{4})\s*[zZ]?(?![A-Za-z0-9_*])", RegexOptions.CultureInvariant);

    private static readonly Regex s_singleTime = new(@"^\d{4}[zZ]?-?$", RegexOptions.CultureInvariant);

    private readonly Matcher _root;

    private StationExpression(Matcher root, bool hasTimeCondition, bool hasCallsignCondition)
    {
        _root = root;
        HasTimeCondition = hasTimeCondition;
        HasCallsignCondition = hasCallsignCondition;
    }

    private enum TokenKind
    {
        Word,
        Time,
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
            var root = parser.ParseAll();
            expression = new StationExpression(root, parser.HasTimeCondition, parser.HasCallsignCondition);
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
    /// <param name="utcNow">The current time in UTC, or null to use the system clock.</param>
    /// <returns>True if the condition is met.</returns>
    public bool Evaluate(IReadOnlyCollection<string>? onlineCallsigns, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        return _root(onlineCallsigns ?? [], (now.Hour * 60) + now.Minute);
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
            else if (char.IsDigit(c) && s_timeRange.Match(text, i) is { Success: true } time)
            {
                tokens.Add(new Token(TokenKind.Time, time.Value, i, ToMinutes(time.Groups[1].Value, i), ToMinutes(time.Groups[2].Value, i)));
                if (tokens[^1].Start == tokens[^1].End)
                {
                    throw new FormatException(
                        $"The start and end of {time.Value.Trim()} are the same (position {i + 1}).");
                }

                i += time.Length;
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

    private readonly record struct Token(TokenKind Kind, string Text, int Position, int Start = 0, int End = 0);

    private sealed class Parser(List<Token> tokens)
    {
        private int _position;

        public bool HasTimeCondition { get; private set; }

        public bool HasCallsignCondition { get; private set; }

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

            if (token.Kind == TokenKind.Time)
            {
                _position++;
                HasTimeCondition = true;
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
                        ? "The condition ends too soon; expected a callsign."
                        : $"Expected a callsign, not '{token.Text}'.",
                    token);
            }

            _position++;
            HasCallsignCondition = true;
            return Callsign(token.Text);
        }
    }

    private static Matcher Callsign(string pattern)
    {
        var regex = new Regex(
            "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return (online, _) => online.Any(regex.IsMatch);
    }

    private static Matcher TimeRange(int start, int end)
    {
        return (_, minutes) => start < end
            ? minutes >= start && minutes < end
            : minutes >= start || minutes < end;
    }

    private static Matcher And(Matcher left, Matcher right)
    {
        return (online, minutes) => left(online, minutes) && right(online, minutes);
    }

    private static Matcher Or(Matcher left, Matcher right)
    {
        return (online, minutes) => left(online, minutes) || right(online, minutes);
    }

    private static Matcher Not(Matcher inner)
    {
        return (online, minutes) => !inner(online, minutes);
    }
}
