// <copyright file="ContractionMatcher.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Applies regex-based <see cref="ContractionMeta"/> entries to ATIS text. The replacement templates support capture
/// group references (<c>$1</c>, <c>${name}</c>) and the functions <c>$group(x)</c>, <c>$digits(x)</c>,
/// <c>$spell(x)</c> and <c>$phonetic(x)</c>, where <c>x</c> is a group reference.
/// </summary>
public static class ContractionMatcher
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(1);

    private static readonly (string Name, string Description)[] s_functionList =
    [
        ("group", "Number in group form (525 = five hundred and twenty-five)"),
        ("digits", "Number one digit at a time (525 = five two five)"),
        ("spell", "Letters and digits separated (ABC = A B C)"),
        ("phonetic", "Phonetic alphabet (ABC = Alpha Bravo Charlie)"),
    ];

    private static readonly HashSet<string> s_functions = new(
        s_functionList.Select(f => f.Name),
        StringComparer.OrdinalIgnoreCase);

    // $$ | $fn($1) | $fn(${name}) | $1 | ${name}
    private static readonly Regex s_token = new(
        @"\$(?:(?<esc>\$)|(?<fn>[A-Za-z]+)\(\s*\$(?:(?<fnnum>\d+)|\{(?<fnname>\w+)\})\s*\)|(?<num>\d+)|\{(?<name>\w+)\})",
        RegexOptions.Compiled);

    /// <summary>
    /// Gets the replacement functions that can wrap a capture group, with a short description of each.
    /// </summary>
    public static IReadOnlyList<(string Name, string Description)> Functions => s_functionList;

    /// <summary>
    /// Replaces contractions in the text: regex contractions first, then <c>@VARIABLE</c> references to plain
    /// contractions. The variable name of a regex contraction is only a label and is never looked up.
    /// </summary>
    /// <param name="text">The text to transform.</param>
    /// <param name="contractions">The station contractions.</param>
    /// <param name="voice">True to use the Voice replacements, otherwise the Text replacements.</param>
    /// <returns>The transformed text.</returns>
    public static string ReplaceContractions(string text, IReadOnlyCollection<ContractionMeta> contractions, bool voice)
    {
        text = Apply(text, contractions, voice);

        return Regex.Replace(text, @"@?(\+?[\w]+(?:_[\w]+)*)", match =>
        {
            var key = match.Groups[1].Value;
            var variable = contractions.FirstOrDefault(v => !v.IsRegex && v.VariableName == key);

            // Leave the token as is if no variable is found.
            return (variable == null ? null : voice ? variable.Voice : variable.Text) ?? match.Value;
        });
    }

    /// <summary>
    /// Applies every regex contraction to the text, using the voice or text replacement.
    /// </summary>
    /// <param name="text">The text to transform.</param>
    /// <param name="contractions">The station contractions; non-regex entries are ignored.</param>
    /// <param name="voice">True to use the <see cref="ContractionMeta.Voice"/> template, otherwise <see cref="ContractionMeta.Text"/>.</param>
    /// <returns>The transformed text.</returns>
    public static string Apply(string text, IEnumerable<ContractionMeta> contractions, bool voice)
    {
        foreach (var contraction in contractions)
        {
            if (!contraction.IsRegex || string.IsNullOrEmpty(contraction.Pattern))
            {
                continue;
            }

            var template = (voice ? contraction.Voice : contraction.Text) ?? string.Empty;

            try
            {
                text = Regex.Replace(
                    text,
                    contraction.Pattern,
                    match => Expand(template, match),
                    RegexOptions.None,
                    s_timeout);
            }
            catch (ArgumentException ex)
            {
                Log.Warning(ex, "Invalid contraction regex pattern: {Pattern}", contraction.Pattern);
            }
            catch (RegexMatchTimeoutException ex)
            {
                Log.Warning(ex, "Timed out applying contraction regex pattern: {Pattern}", contraction.Pattern);
            }
        }

        return text;
    }

    /// <summary>
    /// Checks a regex contraction and explains any problems in plain language.
    /// </summary>
    /// <param name="pattern">The regular expression.</param>
    /// <param name="text">The Text replacement template.</param>
    /// <param name="voice">The Voice replacement template.</param>
    /// <param name="requirePattern">True to report an empty pattern as an error.</param>
    /// <returns>The problems found, each tagged with the field it belongs to (<c>Pattern</c>, <c>Text</c> or <c>Voice</c>).</returns>
    public static IReadOnlyList<(string Field, string Message)> Validate(
        string? pattern,
        string? text,
        string? voice,
        bool requirePattern = false)
    {
        var errors = new List<(string Field, string Message)>();

        Regex? regex = null;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            if (requirePattern)
            {
                errors.Add(("Pattern", "Pattern is required."));
            }
        }
        else
        {
            try
            {
                regex = new Regex(pattern, RegexOptions.None, s_timeout);
                if (regex.IsMatch(string.Empty))
                {
                    errors.Add((
                        "Pattern",
                        "Matches empty text, so it would match everywhere. Require at least one character, e.g. + instead of *."));
                }
            }
            catch (RegexParseException ex)
            {
                errors.Add(("Pattern", DescribeParseError(ex, pattern)));
            }
            catch (ArgumentException ex)
            {
                errors.Add(("Pattern", $"{ex.Message}"));
            }
        }

        var groupNumbers = regex?.GetGroupNumbers().Where(n => n > 0).ToHashSet() ?? [];
        var groupNames = regex?.GetGroupNames().Where(n => !int.TryParse(n, out _)).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        foreach (var (field, template) in new[] { ("Text", text), ("Voice", voice) })
        {
            if (string.IsNullOrEmpty(template))
            {
                continue;
            }

            var message = CheckTemplate(template, regex != null, groupNumbers, groupNames);
            if (message != null)
            {
                errors.Add((field, message));
            }
        }

        return errors;
    }

    private static string? CheckTemplate(string template, bool patternIsValid, HashSet<int> groupNumbers, HashSet<string> groupNames)
    {
        foreach (Match token in s_token.Matches(template))
        {
            if (token.Groups["fn"].Success && !s_functions.Contains(token.Groups["fn"].Value))
            {
                return $"Unknown function '${token.Groups["fn"].Value}'. Use $group, $digits, $spell or $phonetic.";
            }

            if (!patternIsValid)
            {
                continue;
            }

            var number = token.Groups["fnnum"].Success ? token.Groups["fnnum"] : token.Groups["num"];
            if (number.Success && int.TryParse(number.Value, out var n) && !groupNumbers.Contains(n))
            {
                return n == 0
                    ? "There is no $0. Put ( ) around the part to reuse and use $1."
                    : $"No group ${n} in the pattern. Groups are the (bracketed) parts, numbered from the left.";
            }

            var name = token.Groups["fnname"].Success ? token.Groups["fnname"] : token.Groups["name"];
            if (name.Success && !groupNames.Contains(name.Value))
            {
                return $"No group named '{name.Value}'. Name one like this: (?<{name.Value.ToLowerInvariant()}>...).";
            }
        }

        // A function used without a group, e.g. "$spell", "$spell 1" or "$spell(1)" instead of "$spell($1)".
        var bare = Regex.Match(template, @"\$(?<fn>group|digits|spell|phonetic)(?!\(\s*\$(?:\d+|\{\w+\})\s*\))", RegexOptions.IgnoreCase);
        return bare.Success
            ? $"'${bare.Groups["fn"].Value}' needs a group, e.g. ${bare.Groups["fn"].Value}($1)."
            : null;
    }

    private static string DescribeParseError(RegexParseException ex, string pattern)
    {
        return ex.Error switch
        {
            RegexParseError.InsufficientClosingParentheses when FindUnmatched(pattern, '(') is var i and >= 0 =>
                $"Unclosed \"(\" at character {i + 1}, {Near(pattern, i)}. Add a \")\".",
            RegexParseError.InsufficientOpeningParentheses when FindUnmatched(pattern, ')') is var i and >= 0 =>
                $"Extra \")\" at character {i + 1}, {Near(pattern, i)}. Remove it or add a \"(\".",
            RegexParseError.UnterminatedBracket when FindUnmatched(pattern, '[') is var i and >= 0 =>
                $"Unclosed \"[\" at character {i + 1}, {Near(pattern, i)}. Add a \"]\".",
            RegexParseError.QuantifierAfterNothing =>
                $"Nothing to repeat before the \"*\", \"+\" or \"?\" at character {ex.Offset}.",
            RegexParseError.NestedQuantifiersNotParenthesized =>
                $"Two repeat symbols in a row near character {ex.Offset}. Wrap the first one in ( ).",
            RegexParseError.UnescapedEndingBackslash => "The pattern ends with a lone \\. Use \\\\ for a backslash.",
            RegexParseError.UnrecognizedEscape =>
                $"Unknown escape near character {ex.Offset}. Use \\\\ for a literal backslash.",
            RegexParseError.ReversedQuantifierRange => "The numbers in {min,max} are the wrong way round.",
            RegexParseError.ReversedCharacterRange => "A range like [z-a] must go low to high, e.g. [a-z].",
            _ => ex.Message,
        };
    }

    // Shows the start of the pattern from the problem character, e.g. near "(\d/(\d)/(\d)".
    private static string Near(string pattern, int index)
    {
        var snippet = pattern.Substring(index, Math.Min(14, pattern.Length - index));
        return $"near \"{snippet}{(index + snippet.Length < pattern.Length ? "..." : string.Empty)}\"";
    }

    // Finds the first unmatched bracket of the given kind ('(' = unclosed group, ')' = extra close, '[' = unclosed
    // character list), skipping escaped characters and the inside of [...] lists.
    private static int FindUnmatched(string pattern, char kind)
    {
        var open = new Stack<int>();
        var classStart = -1;
        for (var i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '\\')
            {
                i++;
            }
            else if (classStart >= 0)
            {
                if (c == ']')
                {
                    classStart = -1;
                }
            }
            else if (c == '[')
            {
                classStart = i;
            }
            else if (c == '(')
            {
                open.Push(i);
            }
            else if (c == ')')
            {
                if (open.Count == 0)
                {
                    return kind == ')' ? i : -1;
                }

                open.Pop();
            }
        }

        return kind switch
        {
            '[' => classStart,
            '(' => open.Count > 0 ? open.ToArray()[^1] : -1,
            _ => -1,
        };
    }

    private static string Expand(string template, Match match)
    {
        return s_token.Replace(template, token =>
        {
            if (token.Groups["esc"].Success)
            {
                return "$";
            }

            if (token.Groups["fn"].Success)
            {
                var name = token.Groups["fn"].Value;
                if (!s_functions.Contains(name))
                {
                    return token.Value;
                }

                var value = GetGroup(match, token.Groups["fnnum"], token.Groups["fnname"]);
                return ApplyFunction(name.ToLowerInvariant(), value);
            }

            return GetGroup(match, token.Groups["num"], token.Groups["name"]);
        });
    }

    private static string GetGroup(Match match, Group number, Group name)
    {
        if (number.Success)
        {
            return int.TryParse(number.Value, out var index) && index < match.Groups.Count
                ? match.Groups[index].Value
                : string.Empty;
        }

        // Variable text is upper-cased in the editor, so group names are looked up case-insensitively.
        var group = match.Groups.Cast<Group>()
            .FirstOrDefault(g => string.Equals(g.Name, name.Value, StringComparison.OrdinalIgnoreCase));
        return group?.Value ?? string.Empty;
    }

    private static string ApplyFunction(string function, string value)
    {
        switch (function)
        {
            case "group":
                return int.TryParse(value.Replace(",", string.Empty), out var number) ? number.ToGroupForm() : value;
            case "digits":
                return Spell(value, phonetic: false, spellLetters: false);
            case "spell":
                return Spell(value, phonetic: false, spellLetters: true);
            case "phonetic":
                return Spell(value, phonetic: true, spellLetters: true);
            default:
                return value;
        }
    }

    private static string Spell(string value, bool phonetic, bool spellLetters)
    {
        var parts = new List<string>();
        foreach (var ch in value)
        {
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            if (char.IsDigit(ch))
            {
                parts.Add(((int)char.GetNumericValue(ch)).ToWordString());
            }
            else if (char.IsLetter(ch) && spellLetters)
            {
                var upper = char.ToUpperInvariant(ch);
                var word = phonetic ? upper.ToPhonetic() : string.Empty;
                parts.Add(string.IsNullOrEmpty(word) ? upper.ToString() : word);
            }
            else
            {
                parts.Add(ch.ToString());
            }
        }

        return string.Join(" ", parts);
    }
}
