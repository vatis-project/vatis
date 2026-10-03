// <copyright file="TransitionLevelFormatter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Formats the transition level placeholders in ATIS templates.
/// </summary>
public static class TransitionLevelFormatter
{
    /// <summary>
    /// Replaces the <c>{trl[:format]}</c> and <c>{trl|text}</c> placeholders in a template.
    /// The optional format is a digit pattern (e.g. <c>###</c>) that zero-pads the altitude to that many digits.
    /// </summary>
    /// <param name="template">The template text.</param>
    /// <param name="altitude">The transition level altitude.</param>
    /// <returns>The formatted text.</returns>
    public static string Format(string template, int altitude)
    {
        template = Regex.Replace(
            template,
            @"\{trl(?::(?<digits>#+))?\}",
            m => m.Groups["digits"].Success
                ? altitude.ToString(new string('0', m.Groups["digits"].Length))
                : altitude.ToString());

        return Regex.Replace(template, @"\{trl\|text\}", altitude.ToSerialFormat());
    }
}
