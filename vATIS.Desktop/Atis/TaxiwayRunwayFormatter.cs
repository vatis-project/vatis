// <copyright file="TaxiwayRunwayFormatter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Converts taxiway and runway identifiers found in free text (e.g. "TWY B CLSD BTN TWY, C7 AND TWY C10",
/// "DEPG RWYS 1L, 1R", "PAPI OTS 25R", "TWY F BTN D AND E 5") into spoken form.
/// </summary>
public static class TaxiwayRunwayFormatter
{
    private const string Units = @"(?!\s*(?:KTS?|KNOTS|SM|MI|NM|FT|FEET|MILES?|DEG|DEGREES|Z|%)\b)";

    // taxiway id, optionally split by a space ("T 2"), never followed by a unit
    private const string NumberWords =
        @"(?:ONE|TWO|THREE|FOUR|FIVE|SIX|SEVEN|EIGHT|NINER?|TEN|ELEVEN|TWELVE|THIRTEEN|FOURTEEN|FIFTEEN|SIXTEEN|SEVENTEEN|EIGHTEEN|NINETEEN|TWENTY)";

    // also spelled-out numbers ("H ELEVEN"); never a path/hyphenated token ("P/MIL")
    private const string TaxiwayId =
        @"[A-Z]{1,2}(?:\s[0-9]{1,2}\b" + Units + @"|\s" + NumberWords + @"\b|[0-9]{0,2}\b)(?![/\-])";

    private const string RunwayId = @"(?:0?[1-9]|[12][0-9]|3[0-6])";
    private const string Separator = @"(?:\s*,\s*(?:(?:AND|&)\s+)?|\s+(?:AND|&)\s*,?\s*)";

    // runway lists also allow a slash pair such as "15/33"
    private const string RunwaySeparator = @"(?:" + Separator + @"|\s*/\s*)";

    // additional list item separated by whitespace only; must contain a digit ("B11 B10") to avoid words
    private const string SpacedItem = @"\s+[A-Z]{1,2}[0-9]{1,2}\b";
    private const string TaxiwayKeyword = @"(?:TWYS?|TXY|TAXIWAYS?|TAXILANES?)";

    // short English words that look like taxiway identifiers
    private static readonly HashSet<string> s_nonTaxiwayWords = new(StringComparer.Ordinal)
    {
        "AM", "AN", "AS", "AT", "BE", "BY", "DO", "GO", "HE", "IF", "IN", "IS", "IT", "ME", "MY", "NO", "OF", "ON",
        "OR", "SO", "TO", "UP", "US", "WE",
    };

    private static readonly HashSet<string> s_directions = new(StringComparer.Ordinal)
    {
        "NE", "NW", "SE", "SW",
    };

    // TWY/TAXIWAY followed by an id and any list items, e.g. "TWY D, F, K" or "TWYS B2, B3 AND B4"
    private static readonly Regex s_taxiwayList = new(
        $@"\b({TaxiwayKeyword})\b((?:\s+(?:CLSD|CLOSED)\b)?\s*,?\s*)({TaxiwayId})((?:{Separator}{TaxiwayId}|{SpacedItem})*)",
        RegexOptions.Compiled);

    private static readonly Regex s_spelledId = new(
        $@"^([A-Z]{{1,2}})\s({NumberWords})$", RegexOptions.Compiled);

    private static readonly Regex s_taxiwayItem = new($@"\b{TaxiwayId}", RegexOptions.Compiled);

    // "BTN D AND E 5", "BTWN, R AND MID FIELD", "BETWEEN TWY E AND G"
    private static readonly Regex s_between = new(
        $@"\b(BTN|BTWN|BETWEEN)\b(\s*,?\s*)(?:(?<kw1>{TaxiwayKeyword}|RWY|RY|RUNWAY)\b\s*,?\s*)?(?<id1>{TaxiwayId}|[0-9]{{1,2}}[LRC]?\b)" +
        $@"(?:(?<sep>\s*,?\s*AND\b\s*,?\s*)(?:(?<kw2>{TaxiwayKeyword}|RWY|RY|RUNWAY)\b\s*,?\s*)?(?<id2>{TaxiwayId}|[0-9]{{1,2}}[LRC]?\b)(?<more>(?:{SpacedItem})*))?",
        RegexOptions.Compiled);

    private static readonly Regex s_runwayList = new(
        $@"\b(?<kw>R\s?Y|RWYS?|RUNWAYS?)\b,?\s?(?<num>[0-9]{{1,2}})(?<ltr>[LRC]?)\b(?<lr>\s?L/R\b)?(?<items>(?:{RunwaySeparator}{RunwayId}[LRC]?\b{Units}(?!\.[0-9]))*)",
        RegexOptions.Compiled);

    // clause-initial id before a closure/between word: "E1 CLSD", "N BETWEEN RUNWAY 5 AND M", "A E1 CLSD"
    private static readonly Regex s_clauseInitial = new(
        $@"(?<=(?:^|[.;:,])\s*)(?<id>{TaxiwayId}(?:{SpacedItem})*)(?=\s+(?:CLSD|CLOSED|BTN|BTWN|BETWEEN)\b)",
        RegexOptions.Compiled);

    // contextual single identifiers: "AT H7", "FROM K3 TO K1", "INT W", "NORTH OF D"
    private static readonly Regex s_context = new(
        @"\b(?<ctx>(?:NORTH|SOUTH|EAST|WEST)\s+OF|INTERSECTION|INT)\s+(?<id>[A-Z][0-9]{0,2})\b(?![/\-]|\s*[0-9])|" +
        @"\b(?<ctx>AT|FROM|TO)\s+(?<id>[A-Z][0-9]{1,2})\b(?![/\-])",
        RegexOptions.Compiled);

    private static readonly Regex s_runwayItem = new(
        $@"({RunwaySeparator})({RunwayId})([LRC]?)\b", RegexOptions.Compiled);

    private static readonly Regex s_bareRunway = new(
        $@"(?<![\w.^*+#])({RunwayId})([LRC])\b(?![.]?[0-9])", RegexOptions.Compiled);

    /// <summary>
    /// Replaces taxiway and runway identifiers in the given text with their spoken form.
    /// </summary>
    /// <param name="input">The upper-case text to convert.</param>
    /// <param name="leadingZero">Whether single digit runways are prefixed with "zero".</param>
    /// <returns>The converted text.</returns>
    public static string Format(string input, bool leadingZero)
    {
        // between-phrases first: they accept identifiers without a TWY keyword
        input = s_between.Replace(input, m =>
        {
            var id1 = m.Groups["id1"];
            var id2 = m.Groups["id2"];
            var text = m.Value;

            // splice from the end so offsets stay valid
            var more = m.Groups["more"];
            if (more.Success && more.Length > 0 && !m.Groups["kw2"].Success && IsTaxiwayLike(id2.Value))
            {
                var converted = s_taxiwayItem.Replace(more.Value, i => ConvertId(i.Value));
                text = Splice(text, m.Index, more, converted);
            }

            if (id2.Success && !m.Groups["kw2"].Success && IsTaxiwayLike(id2.Value))
            {
                text = Splice(text, m.Index, id2, ConvertId(id2.Value));
            }

            if (!m.Groups["kw1"].Success && IsTaxiwayLike(id1.Value))
            {
                text = Splice(text, m.Index, id1, ConvertId(id1.Value));
            }

            return text;
        });

        input = s_taxiwayList.Replace(input, m =>
        {
            var first = m.Groups[3].Value;
            if (!IsTaxiwayLike(first, afterKeyword: true))
            {
                return m.Value;
            }

            var rest = s_taxiwayItem.Replace(m.Groups[4].Value, i =>
                IsTaxiwayLike(i.Value, afterKeyword: true) ? ConvertId(i.Value) : i.Value);

            return $"{m.Groups[1].Value}{m.Groups[2].Value}{ConvertId(first)}{rest}";
        });

        input = s_clauseInitial.Replace(input, m =>
            IsTaxiwayLike(m.Value) ? s_taxiwayItem.Replace(m.Value, i => ConvertId(i.Value)) : m.Value);

        input = s_context.Replace(input, m =>
        {
            var id = m.Groups["id"].Value;
            return IsTaxiwayLike(id) && id is not ("A" or "I")
                ? $"{m.Groups["ctx"].Value} {ConvertId(id)}"
                : m.Value;
        });

        input = s_runwayList.Replace(input, m =>
        {
            var plural = m.Groups["kw"].Value.EndsWith('S');
            var result = m.Groups["lr"].Success
                ? StringExtensions.RwyNumbersToWords(int.Parse(m.Groups["num"].Value), string.Empty,
                    prefix: true, plural: plural, leadingZero: leadingZero) + " LEFT AND RIGHT"
                : StringExtensions.RwyNumbersToWords(int.Parse(m.Groups["num"].Value), m.Groups["ltr"].Value,
                    prefix: true, plural: plural, leadingZero: leadingZero);

            result += s_runwayItem.Replace(m.Groups["items"].Value, i =>
            {
                var separator = i.Groups[1].Value.Contains('/')
                    ? ", "
                    : i.Groups[1].Value.Replace("&", "AND");
                var words = StringExtensions.RwyNumbersToWords(
                    int.Parse(i.Groups[2].Value), i.Groups[3].Value, leadingZero: leadingZero);
                return separator + words;
            });

            return result;
        });

        // bare designators with a side letter, e.g. "PAPI OTS 25R"
        input = s_bareRunway.Replace(input, m =>
            StringExtensions.RwyNumbersToWords(int.Parse(m.Groups[1].Value), m.Groups[2].Value,
                leadingZero: leadingZero));

        return input;
    }

    private static bool IsTaxiwayLike(string id, bool afterKeyword = false)
    {
        // must start with a letter and not be a short English word
        if (id.Length == 0 || !char.IsLetter(id[0]))
        {
            return false;
        }

        var letters = new string(id.TakeWhile(char.IsLetter).ToArray());

        // compass directions are taxiway names only when a TWY/TAXIWAY keyword introduces them
        return !s_nonTaxiwayWords.Contains(letters) && (afterKeyword || !s_directions.Contains(letters));
    }

    private static string ConvertId(string id)
    {
        // "H ELEVEN": phonetic letters followed by an already spelled-out number
        var spelled = s_spelledId.Match(id);
        return spelled.Success
            ? $"{spelled.Groups[1].Value.ToAlphaNumericWordGroup()} {spelled.Groups[2].Value}"
            : id.ToAlphaNumericWordGroup();
    }

    private static string Splice(string text, int matchIndex, Group group, string replacement)
    {
        var start = group.Index - matchIndex;
        return string.Concat(text.AsSpan(0, start), replacement, text.AsSpan(start + group.Length));
    }
}
