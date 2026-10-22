// <copyright file="PatternSampleGenerator.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Generates a sample of text that matches a regular expression, so a pattern can be previewed without typing
/// sample text. It understands the common syntax (literals, escapes, character classes, groups, alternation,
/// quantifiers and back-references); anything it cannot produce a verified match for is reported as a failure.
/// </summary>
public static class PatternSampleGenerator
{
    private const int MaxLength = 120;

    private static readonly (char Low, char High)[] s_digits = [('0', '9')];
    private static readonly (char Low, char High)[] s_letters = [('A', 'Z')];
    private static readonly (char Low, char High)[] s_word = [('A', 'Z'), ('0', '9')];
    private static readonly (char Low, char High)[] s_space = [(' ', ' ')];

    /// <summary>
    /// Tries to generate text that the pattern matches.
    /// </summary>
    /// <param name="pattern">The regular expression.</param>
    /// <param name="random">The source of randomness, so the same seed gives the same sample.</param>
    /// <param name="sample">The generated text.</param>
    /// <returns>True if a sample was generated and the pattern matches it.</returns>
    public static bool TryGenerate(string? pattern, Random random, out string sample)
    {
        sample = string.Empty;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        try
        {
            var parser = new Parser(pattern, random);
            var result = parser.Generate().Trim();

            // Only offer a sample the pattern really matches.
            if (result.Length is 0 or > MaxLength ||
                !Regex.IsMatch(result, pattern, RegexOptions.None, TimeSpan.FromSeconds(1)))
            {
                return false;
            }

            sample = result;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException or FormatException
                                       or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a random source that always gives the same sequence for the same pattern, so a sample does not
    /// change on every keystroke.
    /// </summary>
    /// <param name="pattern">The regular expression.</param>
    /// <returns>A seeded <see cref="Random"/>.</returns>
    public static Random SeededFor(string pattern)
    {
        var hash = 17;
        foreach (var c in pattern)
        {
            hash = unchecked((hash * 31) + c);
        }

        return new Random(hash);
    }

    private sealed class Parser
    {
        private readonly string _pattern;
        private readonly Random _random;
        private readonly Dictionary<int, string> _captures = [];
        private readonly Dictionary<string, int> _names = new(StringComparer.OrdinalIgnoreCase);
        private int _position;
        private int _groupCount;

        public Parser(string pattern, Random random)
        {
            _pattern = pattern;
            _random = random;
        }

        public string Generate()
        {
            var node = ParseAlternation();
            if (_position < _pattern.Length)
            {
                throw new FormatException("Unbalanced pattern.");
            }

            var sb = new StringBuilder();
            Emit(node, sb);
            return sb.ToString();
        }

        private Node ParseAlternation()
        {
            var options = new List<Node> { ParseSequence() };
            while (_position < _pattern.Length && _pattern[_position] == '|')
            {
                _position++;
                options.Add(ParseSequence());
            }

            return options.Count == 1 ? options[0] : new Alt(options);
        }

        private Node ParseSequence()
        {
            var items = new List<Node>();
            while (_position < _pattern.Length && _pattern[_position] is not ('|' or ')'))
            {
                items.Add(ParseQuantified());
            }

            return new Seq(items);
        }

        private Node ParseQuantified()
        {
            var atom = ParseAtom();
            if (_position >= _pattern.Length)
            {
                return atom;
            }

            int min;
            int max;
            switch (_pattern[_position])
            {
                case '*':
                    (min, max) = (1, 2);
                    _position++;
                    break;
                case '+':
                    (min, max) = (1, 3);
                    _position++;
                    break;
                case '?':
                    (min, max) = (1, 1);
                    _position++;
                    break;
                case '{' when TryParseBraces(out min, out max):
                    break;
                default:
                    return atom;
            }

            // Skip a lazy (?) or possessive (+) marker.
            if (_position < _pattern.Length && _pattern[_position] is '?' or '+')
            {
                _position++;
            }

            return new Repeat(atom, min, max);
        }

        private bool TryParseBraces(out int min, out int max)
        {
            min = max = 0;
            var match = Regex.Match(_pattern[_position..], @"^\{(\d+)(?:(,)(\d*))?\}");
            if (!match.Success)
            {
                return false;
            }

            min = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            max = !match.Groups[2].Success
                ? min
                : match.Groups[3].Length == 0 ? min + 2 : int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

            // Show optional parts, and keep the sample short.
            if (min == 0 && max > 0)
            {
                min = 1;
            }

            max = Math.Min(Math.Max(max, min), min + 4);
            _position += match.Length;
            return true;
        }

        private Node ParseAtom()
        {
            var c = _pattern[_position++];
            switch (c)
            {
                case '(':
                    return ParseGroup();
                case '[':
                    return ParseClass();
                case '\\':
                    return ParseEscape(inClass: false);
                case '.':
                    return new CharSet(s_letters, Negated: false);
                case '^' or '$':
                    return new Seq([]);
                default:
                    return new Lit(c);
            }
        }

        private Node ParseGroup()
        {
            int? index = null;
            string? name = null;
            var zeroWidth = false;

            if (_position < _pattern.Length && _pattern[_position] == '?')
            {
                var rest = _pattern[_position..];
                var named = Regex.Match(rest, @"^\?(?:<(\w+)>|'(\w+)')");
                if (rest.StartsWith("?:", StringComparison.Ordinal))
                {
                    _position += 2;
                }
                else if (rest.StartsWith("?=", StringComparison.Ordinal) || rest.StartsWith("?!", StringComparison.Ordinal))
                {
                    _position += 2;
                    zeroWidth = true;
                }
                else if (rest.StartsWith("?<=", StringComparison.Ordinal) || rest.StartsWith("?<!", StringComparison.Ordinal))
                {
                    _position += 3;
                    zeroWidth = true;
                }
                else if (named.Success)
                {
                    _position += named.Length;
                    name = named.Groups[1].Success ? named.Groups[1].Value : named.Groups[2].Value;
                    index = ++_groupCount;
                    _names[name] = index.Value;
                }
                else
                {
                    // Inline options such as (?i) or (?i:...).
                    var flags = Regex.Match(rest, @"^\?[imnsx\-]+(\)|:)");
                    if (!flags.Success)
                    {
                        throw new FormatException("Unsupported group.");
                    }

                    _position += flags.Length;
                    if (flags.Groups[1].Value == ")")
                    {
                        return new Seq([]);
                    }
                }
            }
            else
            {
                index = ++_groupCount;
            }

            var inner = ParseAlternation();
            if (_position >= _pattern.Length || _pattern[_position] != ')')
            {
                throw new FormatException("Unclosed group.");
            }

            _position++;
            return zeroWidth ? new Seq([]) : new Group(inner, index);
        }

        private Node ParseClass()
        {
            var negated = _position < _pattern.Length && _pattern[_position] == '^';
            if (negated)
            {
                _position++;
            }

            var ranges = new List<(char Low, char High)>();
            var first = true;
            while (_position < _pattern.Length && (_pattern[_position] != ']' || first))
            {
                first = false;
                var c = _pattern[_position++];
                if (c == '\\')
                {
                    var escaped = ParseEscape(inClass: true);
                    if (escaped is CharSet set)
                    {
                        ranges.AddRange(set.Ranges);
                    }
                    else if (escaped is Lit lit)
                    {
                        ranges.Add((lit.Value, lit.Value));
                    }

                    continue;
                }

                if (_position + 1 < _pattern.Length && _pattern[_position] == '-' && _pattern[_position + 1] != ']')
                {
                    _position++;
                    var high = _pattern[_position++];
                    if (high == '\\')
                    {
                        high = ParseEscape(inClass: true) is Lit end ? end.Value : c;
                    }

                    ranges.Add((c, (char)Math.Max(c, high)));
                }
                else
                {
                    ranges.Add((c, c));
                }
            }

            if (_position >= _pattern.Length)
            {
                throw new FormatException("Unclosed character class.");
            }

            _position++;
            return new CharSet(ranges.ToArray(), negated);
        }

        private Node ParseEscape(bool inClass)
        {
            if (_position >= _pattern.Length)
            {
                throw new FormatException("Trailing backslash.");
            }

            var c = _pattern[_position++];
            switch (c)
            {
                case 'd':
                    return new CharSet(s_digits, Negated: false);
                case 'D':
                    return new CharSet(s_letters, Negated: false);
                case 'w':
                    return new CharSet(s_word, Negated: false);
                case 'W' or 's':
                    return new CharSet(s_space, Negated: false);
                case 'S':
                    return new CharSet(s_letters, Negated: false);
                case 'n' or 't' or 'r':
                    return new Lit(' ');
                case 'b' or 'B' or 'A' or 'Z' or 'z' or 'G' when !inClass:
                    return new Seq([]);
                case >= '1' and <= '9' when !inClass:
                    return new BackReference(c - '0', null);
                case 'k' when !inClass && _position < _pattern.Length && _pattern[_position] is '<' or '\'':
                    var close = _pattern[_position] == '<' ? '>' : '\'';
                    var end = _pattern.IndexOf(close, _position + 1);
                    if (end < 0)
                    {
                        throw new FormatException("Bad back-reference.");
                    }

                    var name = _pattern[(_position + 1)..end];
                    _position = end + 1;
                    return new BackReference(0, name);
                case 'x' when _position + 1 < _pattern.Length:
                    var hex = _pattern.Substring(_position, 2);
                    _position += 2;
                    return new Lit((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                default:
                    return new Lit(c);
            }
        }

        private void Emit(Node node, StringBuilder sb)
        {
            if (sb.Length > MaxLength)
            {
                return;
            }

            switch (node)
            {
                case Seq seq:
                    foreach (var item in seq.Items)
                    {
                        Emit(item, sb);
                    }

                    break;
                case Alt alt:
                    // Every option is generated so group numbers stay in step; one is kept.
                    var texts = alt.Options.Select(o =>
                    {
                        var option = new StringBuilder();
                        Emit(o, option);
                        return option.ToString();
                    }).ToList();
                    sb.Append(texts[_random.Next(texts.Count)]);
                    break;
                case Lit lit:
                    sb.Append(lit.Value);
                    break;
                case CharSet set:
                    sb.Append(set.Pick(_random));
                    break;
                case Repeat repeat:
                    var count = _random.Next(repeat.Min, repeat.Max + 1);
                    for (var i = 0; i < count; i++)
                    {
                        Emit(repeat.Inner, sb);
                    }

                    break;
                case Group group:
                    var inner = new StringBuilder();
                    Emit(group.Inner, inner);
                    if (group.Index is { } index)
                    {
                        _captures[index] = inner.ToString();
                    }

                    sb.Append(inner);
                    break;
                case BackReference reference:
                    var key = reference.Name != null && _names.TryGetValue(reference.Name, out var named)
                        ? named
                        : reference.Index;
                    sb.Append(_captures.GetValueOrDefault(key, string.Empty));
                    break;
            }
        }
    }

    private abstract record Node;

    private sealed record Seq(List<Node> Items) : Node;

    private sealed record Alt(List<Node> Options) : Node;

    private sealed record Lit(char Value) : Node;

    private sealed record Repeat(Node Inner, int Min, int Max) : Node;

    private sealed record Group(Node Inner, int? Index) : Node;

    private sealed record BackReference(int Index, string? Name) : Node;

    private sealed record CharSet((char Low, char High)[] Ranges, bool Negated) : Node
    {
        public char Pick(Random random)
        {
            var candidates = new List<char>();
            if (Negated)
            {
                // Pick an uppercase letter or digit that the class excludes.
                foreach (var c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
                {
                    if (!Ranges.Any(r => c >= r.Low && c <= r.High))
                    {
                        candidates.Add(c);
                    }
                }

                return candidates.Count == 0 ? '?' : candidates[random.Next(candidates.Count)];
            }

            foreach (var (low, high) in Ranges)
            {
                for (var c = (int)low; c <= high && candidates.Count < 512; c++)
                {
                    candidates.Add((char)c);
                }
            }

            return candidates.Count == 0 ? '?' : candidates[random.Next(candidates.Count)];
        }
    }
}
