using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis.Extensions;
public static class StringExtensions
{
    public static string StripNewLineChars(this string? input)
    {
        return Regex.Replace(input ?? "", @"\t|\n|\r", "");
    }

    /// <summary>
    /// Converts alphanumeric strings to human-readable format. Useful for translating taxiways.
    /// </summary>
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

            return "";
        }
        else
        {
            return string.Join(" ", alpha.Select(x => Alphabet[x]).ToArray());
        }
    }

    /// <summary>
    /// Translates single letter to phonetic alphabet variant.
    /// For example, the character "C" would return "Charlie".
    /// </summary>
    /// <param name="x">Single alpha character A-Z</param>
    public static string ToPhonetic(this char x)
    {
        if (Alphabet.ContainsKey(x))
        {
            return Alphabet[x];
        }
        return "";
    }

    /// <summary>
    /// Translates runway numbers to human-readable format for the voice synthesizer.
    /// For example, RWY 25R would translate to "Runway Two Five Right"
    /// </summary>
    /// <param name="number">The runway number 1-360</param>
    /// <param name="identifier">The runway position identifier (L, R, C, null)</param>
    /// <param name="prefix"></param>
    /// <param name="plural"></param>
    /// <param name="leadingZero"></param>
    /// <returns></returns>
    public static string RwyNumbersToWords(int number, string identifier, bool prefix = false, bool plural = false, bool leadingZero = false)
    {
        var words = "";
        var result = "";

        if (leadingZero && number < 10)
            words += "zero ";

        if (number is >= 1 and <= 36)
        {
            var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "niner", "one zero", "one one", "one two", "one three", "one four", "one five", "one six", "one seven", "one eight", "one niner", "two zero", "two one", "two two", "two three", "two four", "two five", "two six", "two seven", "two eight", "two niner", "three zero", "three one", "three two", "three three", "three four", "three five", "three six" };

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
                ident = "";
                break;
        }

        if (prefix && plural)
            result = $" runways {words} {ident}";
        if (prefix && !plural)
            result = $" runway {words} {ident}";
        else if (!prefix && !plural)
            result = $" {words} {ident}";

        return result.ToUpper();
    }

    public static string RandomLetter()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var random = new Random();
        return new string(Enumerable.Range(1, 1).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    public static bool IsValidUrl(this string value)
    {
        var pattern = new Regex(@"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$");
        return pattern.IsMatch(value.Trim());
    }

    /// <summary>
    /// Phonetic Alphabet
    /// </summary>
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
        { 'Z', "Zulu" }
    };
}
