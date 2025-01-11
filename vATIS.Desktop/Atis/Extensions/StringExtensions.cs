// <copyright file="StringExtensions.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Provides extension methods for string values to convert them into various textual and formatted representations.
/// </summary>
public static class StringExtensions
{
    private static readonly Random Random = new();

    private static Dictionary<char, string> Alphabet => new()
    {
        { 'A', "Alpha" },
        { 'B', "Bravo" },
        { 'C', "Charlie" },
        { 'D', "Delta" },
        { 'E', "Echo" },
        { 'F', "Foxtrot" },
        { 'G', "Golf" },
        { 'H', "Hotel" },
        { 'I', "India" },
        { 'J', "Juliet" },
        { 'K', "Kilo" },
        { 'L', "Lima" },
        { 'M', "Mike" },
        { 'N', "November" },
        { 'O', "Oscar" },
        { 'P', "Papa" },
        { 'Q', "Quebec" },
        { 'R', "Romeo" },
        { 'S', "Sierra" },
        { 'T', "Tango" },
        { 'U', "Uniform" },
        { 'V', "Victor" },
        { 'W', "Whiskey" },
        { 'X', "X-Ray" },
        { 'Y', "Yankee" },
        { 'Z', "Zulu" },
    };

    /// <summary>
    /// Strips new line characters from a string.
    /// </summary>
    /// <param name="input">The string to strip new line characters from.</param>
    /// <returns>The string without new line characters.</returns>
    public static string StripNewLineChars(this string? input)
    {
        return Regex.Replace(input ?? string.Empty, @"\t|\n|\r", string.Empty);
    }

    /// <summary>
    /// Converts a string to an alphanumeric word group.
    /// </summary>
    /// <param name="s">The string to convert.</param>
    /// <returns>The alphanumeric word group.</returns>
    public static string ToAlphaNumericWordGroup(this string s)
    {
        var alpha = new string(s.Where(char.IsLetter).ToArray());
        var num = new string(s.Where(char.IsNumber).ToArray());
        if (num.Length > 0)
        {
            if (int.TryParse(num, out var numOut))
            {
                return $"{string.Join(" ", alpha.Select(x => Alphabet[x]).ToArray())} {numOut.ToGroupForm()}";
            }

            return string.Empty;
        }

        return string.Join(" ", alpha.Select(x => Alphabet[x]).ToArray());
    }

    /// <summary>
    /// Converts a string to a phonetic representation.
    /// </summary>
    /// <param name="x">The character to convert.</param>
    /// <returns>The phonetic representation of the character.</returns>
    public static string ToPhonetic(this char x)
    {
        if (Alphabet.ContainsKey(x))
        {
            return Alphabet[x];
        }

        return string.Empty;
    }

    /// <summary>
    /// Translates runway numbers to human-readable format for the voice synthesizer.
    /// For example, RWY 25R would translate to "Runway Two Five Right".
    /// </summary>
    /// <param name="number">The runway number to translate.</param>
    /// <param name="identifier">The runway identifier (L, R, C).</param>
    /// <param name="prefix">Whether to prefix the runway number with "runway".</param>
    /// <param name="plural">Whether to pluralize the runway number.</param>
    /// <param name="leadingZero">Whether to prefix the runway number with a leading zero if less than 10.</param>
    /// <returns>The runway number in human-readable format.</returns>
    public static string RwyNumbersToWords(
        int number,
        string identifier,
        bool prefix = false,
        bool plural = false,
        bool leadingZero = false)
    {
        var words = string.Empty;
        var result = string.Empty;

        if (leadingZero && number < 10)
        {
            words += "zero ";
        }

        if (number is >= 1 and <= 36)
        {
            var unitsMap = new[]
            {
                "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "niner", "one zero", "one one",
                "one two", "one three", "one four", "one five", "one six", "one seven", "one eight", "one niner",
                "two zero", "two one", "two two", "two three", "two four", "two five", "two six", "two seven",
                "two eight", "two niner", "three zero", "three one", "three two", "three three", "three four",
                "three five", "three six",
            };

            words += unitsMap[number];
        }

        string ident;
        switch (identifier)
        {
            case "L":
                ident = "left";
                break;
            case "R":
                ident = "right";
                break;
            case "C":
                ident = "center";
                break;
            default:
                ident = string.Empty;
                break;
        }

        if (prefix && plural)
        {
            result = $" runways {words} {ident}";
        }

        if (prefix && !plural)
        {
            result = $" runway {words} {ident}";
        }
        else if (!prefix && !plural)
        {
            result = $" {words} {ident}";
        }

        return result.ToUpper();
    }

    /// <summary>
    /// Generates a random uppercase letter from A to Z.
    /// </summary>
    /// <returns>A randomly generated uppercase letter.</returns>
    public static string RandomLetter()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return chars[Random.Next(chars.Length)].ToString();
    }

    /// <summary>
    /// Checks if a string is a valid URL.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is a valid URL; otherwise, false.</returns>
    public static bool IsValidUrl(this string value)
    {
        var pattern = new Regex(@"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$");
        return pattern.IsMatch(value.Trim());
    }
}
