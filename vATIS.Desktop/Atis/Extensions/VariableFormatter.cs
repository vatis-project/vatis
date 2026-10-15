// <copyright file="VariableFormatter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Applies user-defined <see cref="OutputFormat"/> options to a rendered variable value.
/// </summary>
public static class VariableFormatter
{
    /// <summary>
    /// Applies the format to a rendered variable.
    /// </summary>
    /// <param name="rendered">The built-in rendering, used when the style is <see cref="NumberStyle.Default"/>.</param>
    /// <param name="digits">The plain numeric value (e.g. "1013" or "29.92").</param>
    /// <param name="format">The format to apply, or null for none.</param>
    /// <param name="uppercase">True to uppercase word renderings (text ATIS).</param>
    /// <param name="pronounceDecimal">True to speak the decimal point as "point" in serial form.</param>
    /// <returns>The formatted value.</returns>
    public static string Apply(
        string rendered, string digits, OutputFormat? format, bool uppercase = false, bool pronounceDecimal = false)
    {
        if (format == null)
            return rendered;

        var body = format.Style switch
        {
            NumberStyle.Digits => digits,
            NumberStyle.Serial => ToWords(digits, false, pronounceDecimal),
            NumberStyle.Group => ToWords(digits, true, pronounceDecimal),
            _ => rendered,
        };

        if (uppercase && format.Style is NumberStyle.Serial or NumberStyle.Group)
            body = body.ToUpperInvariant();

        return body + format.Suffix;
    }

    private static string ToWords(string digits, bool group, bool pronounceDecimal)
    {
        if (group && int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            return number.ToGroupForm();

        return digits.ToSerialFormat(pronounceDecimal) ?? digits;
    }
}
