// <copyright file="StaticDefinitionJoiner.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Joins static airport condition and NOTAM definitions using a configurable separator.
/// </summary>
public static class StaticDefinitionJoiner
{
    /// <summary>
    /// The separator used when none has been configured.
    /// </summary>
    public const string DefaultSeparator = ". ";

    /// <summary>
    /// Returns the configured separator, or the default when it is null or empty.
    /// </summary>
    /// <param name="separator">The configured separator.</param>
    /// <returns>The separator to use.</returns>
    public static string Normalize(string? separator)
    {
        return string.IsNullOrEmpty(separator) ? DefaultSeparator : separator;
    }

    /// <summary>
    /// Determines whether the separator is the default one.
    /// </summary>
    /// <param name="separator">The configured separator.</param>
    /// <returns>True if the default separator is in effect.</returns>
    public static bool IsDefault(string? separator)
    {
        return Normalize(separator) == DefaultSeparator;
    }

    /// <summary>
    /// Determines whether any separator in effect differs from the default, in which case spacing must be preserved.
    /// </summary>
    /// <param name="definitions">The enabled definitions, in order.</param>
    /// <param name="stationSeparator">The station separator.</param>
    /// <returns>True if a custom separator is used between any two definitions or as the station default.</returns>
    public static bool HasCustomSeparators(IReadOnlyList<StaticDefinition> definitions, string? stationSeparator)
    {
        if (!IsDefault(stationSeparator))
        {
            return true;
        }

        for (var i = 0; i < definitions.Count - 1; i++)
        {
            if (!IsDefault(definitions[i].SeparatorAfter ?? stationSeparator))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Joins the given definitions. Each separator belongs to the definition before it: its own
    /// <see cref="StaticDefinition.SeparatorAfter"/> if set, otherwise the station separator. Custom separators are
    /// preserved exactly as typed.
    /// </summary>
    /// <param name="definitions">The enabled definitions, in order.</param>
    /// <param name="stationSeparator">The station separator.</param>
    /// <param name="trimTrailingPeriod">Whether to trim a trailing period from each definition's text.</param>
    /// <returns>The joined string.</returns>
    public static string Join(
        IReadOnlyList<StaticDefinition> definitions, string? stationSeparator, bool trimTrailingPeriod = false)
    {
        var custom = HasCustomSeparators(definitions, stationSeparator);
        var sb = new StringBuilder();
        for (var i = 0; i < definitions.Count; i++)
        {
            var text = definitions[i].Text;
            if (trimTrailingPeriod)
            {
                text = text.TrimEnd('.');
            }

            sb.Append(custom ? CleanUp(text).Trim() : text);
            if (i < definitions.Count - 1)
            {
                sb.Append(Normalize(definitions[i].SeparatorAfter ?? stationSeparator));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Cleans free text that will be combined with a custom-separated block. Left untouched when no custom separators
    /// are used because the whole string is cleaned afterwards by <see cref="Finish"/>.
    /// </summary>
    /// <param name="text">The free text.</param>
    /// <param name="custom">Whether custom separators are in use.</param>
    /// <returns>The prepared text.</returns>
    public static string Prepare(string? text, bool custom)
    {
        text ??= string.Empty;
        return custom ? CleanUp(text) : text;
    }

    /// <summary>
    /// Applies final punctuation cleanup to the combined string. Skipped when custom separators are used so that
    /// their spacing is not stripped.
    /// </summary>
    /// <param name="combined">The combined string.</param>
    /// <param name="custom">Whether custom separators are in use.</param>
    /// <returns>The cleaned string.</returns>
    public static string Finish(string combined, bool custom)
    {
        return custom ? combined : CleanUp(combined);
    }

    private static string CleanUp(string text)
    {
        text = Regex.Replace(text, @"[!?.]*([!?.])", "$1");
        return Regex.Replace(text, "\\s+([.,!\":])", "$1");
    }
}
