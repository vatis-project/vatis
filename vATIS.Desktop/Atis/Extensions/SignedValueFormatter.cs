// <copyright file="SignedValueFormatter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Formats signed values (temperature, dewpoint) for text ATIS templates.
/// </summary>
internal static class SignedValueFormatter
{
    /// <summary>
    /// Replaces the <c>{prefix_symbol}</c> and <c>{variable[:format]}</c> placeholders in a text template.
    /// The format is an optional digit pattern (e.g. <c>##</c>) followed by an optional negative prefix:
    /// <c>M</c> removes the prefix, <c>-</c> uses a dash, and <c>'text'</c> uses custom text (e.g. <c>'MS'</c>).
    /// When no prefix option is given, negative values are prefixed with "M".
    /// </summary>
    /// <param name="format">The text template.</param>
    /// <param name="variable">The variable name, e.g. "temp" or "dewpoint".</param>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted text.</returns>
    public static string Format(string format, string variable, double value)
    {
        format = Regex.Replace(format, @"\{prefix_symbol\}", value < 0 ? "-" : "+", RegexOptions.IgnoreCase);

        var match = Regex.Match(format, @"\{" + variable + @"(?::(?<digits>#*)(?<neg>M|-|'[^']*')?)?\}");
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid {variable} format string: {format}");
        }

        int digitCount = match.Groups["digits"].Value.Length;
        if (digitCount == 0) digitCount = 2;

        string neg = match.Groups["neg"].Value;
        string prefix = neg switch
        {
            "M" => string.Empty,
            "" => "M",
            "-" => "-",
            _ => neg.Trim('\''),
        };

        string number = Math.Abs(value).ToString(new string('0', digitCount));
        return format.Replace(match.Value, (value < 0 ? prefix : string.Empty) + number);
    }
}
