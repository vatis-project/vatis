// <copyright file="SpokenTextDigitsConverter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Converts spoken numbers, runways and letters in a voice script back to digits and single letters,
/// so the script is easier to read aloud.
/// </summary>
public static class SpokenTextDigitsConverter
{
    private const string DigitWord = @"(?:zero|one|two|three|four|five|six|seven|eight|niner|nine)";
    private const string DigitRun = $@"{DigitWord}(?:\s+{DigitWord})*";

    // Word boundaries that also reject a hyphen, so "one-two thousand" is left alone.
    private const string Start = @"(?<![\w-])";
    private const string End = @"(?![\w-])";

    private static readonly Dictionary<string, char> s_digits = new(System.StringComparer.OrdinalIgnoreCase)
    {
        ["zero"] = '0',
        ["one"] = '1',
        ["two"] = '2',
        ["three"] = '3',
        ["four"] = '4',
        ["five"] = '5',
        ["six"] = '6',
        ["seven"] = '7',
        ["eight"] = '8',
        ["nine"] = '9',
        ["niner"] = '9',
    };

    private static readonly Dictionary<string, char> s_letters = new(System.StringComparer.OrdinalIgnoreCase)
    {
        ["alpha"] = 'A', ["bravo"] = 'B', ["charlie"] = 'C', ["delta"] = 'D', ["echo"] = 'E', ["foxtrot"] = 'F',
        ["golf"] = 'G', ["hotel"] = 'H', ["india"] = 'I', ["juliet"] = 'J', ["kilo"] = 'K', ["lima"] = 'L',
        ["mike"] = 'M', ["november"] = 'N', ["oscar"] = 'O', ["papa"] = 'P', ["quebec"] = 'Q', ["romeo"] = 'R',
        ["sierra"] = 'S', ["tango"] = 'T', ["uniform"] = 'U', ["victor"] = 'V', ["whiskey"] = 'W', ["x-ray"] = 'X',
        ["yankee"] = 'Y', ["zulu"] = 'Z',
    };

    private static readonly Regex s_thousands = new(
        $@"{Start}({DigitRun})\s+thousand(?:\s+({DigitWord})\s+hundred)?{End}", RegexOptions.IgnoreCase);

    private static readonly Regex s_hundreds = new($@"{Start}({DigitWord})\s+hundred{End}", RegexOptions.IgnoreCase);

    private static readonly Regex s_numbers = new(
        $@"{Start}({DigitRun})(?:\s+(?:decimal|point)\s+({DigitRun}))?{End}", RegexOptions.IgnoreCase);

    private static readonly Regex s_runwaySide = new(@"(?<=\d)\s+(left|right|center)\b", RegexOptions.IgnoreCase);

    private static readonly Regex s_letter = new(
        @"\b(info|information|taxiway|taxiways)\s+(" + string.Join("|", s_letters.Keys.Select(Regex.Escape)) + @")\b",
        RegexOptions.IgnoreCase);

    /// <summary>
    /// Replaces spoken digits, group numbers, runway sides and phonetic letters in a voice script.
    /// For example, "wind zero niner zero at eight" becomes "wind 090 at 8".
    /// </summary>
    /// <param name="script">The voice script.</param>
    /// <returns>The script with numbers and letters written out as digits and letters.</returns>
    public static string Convert(string script)
    {
        // Group form first: "four thousand two hundred" -> 4200.
        script = s_thousands.Replace(script, m =>
        {
            // "two zero thousand" -> 20000, "four thousand two hundred" -> 4200.
            var value = long.Parse(ToDigits(m.Groups[1].Value), CultureInfo.InvariantCulture) * 1000;
            if (m.Groups[2].Success)
            {
                value += Digit(m.Groups[2].Value) * 100;
            }

            return value.ToString(CultureInfo.InvariantCulture);
        });

        script = s_hundreds.Replace(script,
            m => (Digit(m.Groups[1].Value) * 100).ToString(CultureInfo.InvariantCulture));

        // Serial form: "two niner niner four" -> 2994, "one two one decimal seven" -> 121.7.
        script = s_numbers.Replace(script, m =>
        {
            var whole = ToDigits(m.Groups[1].Value);
            return m.Groups[2].Success ? $"{whole}.{ToDigits(m.Groups[2].Value)}" : whole;
        });

        script = s_runwaySide.Replace(script, m => m.Groups[1].Value[..1].ToUpperInvariant());

        // The information letter and taxiway letters: "information uniform" -> "information U".
        return s_letter.Replace(script,
            m => $"{m.Groups[1].Value} {s_letters[m.Groups[2].Value]}");
    }

    private static int Digit(string word) => s_digits[word] - '0';

    private static string ToDigits(string words)
    {
        return new string(words.Split((char[]?)null, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(w => s_digits[w]).ToArray());
    }
}
