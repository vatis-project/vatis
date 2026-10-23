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
    /// Returns the configured separator, or the default when it is null or empty. The typed sequence <c>\n</c> is
    /// replaced with a line break.
    /// </summary>
    /// <param name="separator">The configured separator.</param>
    /// <returns>The separator to use.</returns>
    public static string Normalize(string? separator)
    {
        if (string.IsNullOrEmpty(separator))
        {
            return DefaultSeparator;
        }

        return separator.Replace("\\n", "\n");
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
            if (!definitions[i].IsSpacer && !IsDefault(SeparatorFor(definitions[i], stationSeparator)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Joins the given definitions. Each separator belongs to the definition before it: its own
    /// <see cref="StaticDefinition.SeparatorAfter"/> if set, otherwise the station separator. Custom separators are
    /// preserved exactly as typed. A blank line definition adds an empty line between its neighbors instead of text,
    /// and a new line definition adds a single line break. Both are ignored at the start or end.
    /// </summary>
    /// <param name="definitions">The enabled definitions, in order.</param>
    /// <param name="stationSeparator">The station separator.</param>
    /// <param name="trimTrailingPeriod">Whether to trim a trailing period from each definition's text.</param>
    /// <param name="terminate">Whether nothing follows the last definition. If its separator ends with a line break,
    /// the rest of that separator (e.g. the period in <c>.\n</c>) is kept after it without the line break.</param>
    /// <returns>The joined string.</returns>
    public static string Join(
        IReadOnlyList<StaticDefinition> definitions, string? stationSeparator, bool trimTrailingPeriod = false,
        bool terminate = false)
    {
        var custom = HasCustomSeparators(definitions, stationSeparator);

        // Collect the definitions with text, and how many line breaks the blank and new line definitions before each
        // add in place of the separator.
        var items = new List<(StaticDefinition Definition, string Text, int Breaks)>();
        var blankLines = 0;
        var newLines = 0;
        foreach (var definition in definitions)
        {
            if (definition.IsSpacer)
            {
                if (items.Count > 0)
                {
                    if (definition.IsNewLine)
                    {
                        newLines++;
                    }
                    else
                    {
                        blankLines++;
                    }
                }

                continue;
            }

            var text = definition.Text;
            if (trimTrailingPeriod)
            {
                text = text.TrimEnd('.');
            }

            // A blank line is one more break than a new line, so two blank lines in a row leave two empty lines.
            var breaks = blankLines + newLines > 0 ? newLines + blankLines + (blankLines > 0 ? 1 : 0) : 0;
            items.Add((definition, custom ? CleanUp(text).Trim() : text, breaks));
            blankLines = 0;
            newLines = 0;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < items.Count; i++)
        {
            var (definition, text, _) = items[i];
            var separator = Normalize(SeparatorFor(definition, stationSeparator));
            var isLast = i == items.Count - 1;

            // The separator supplies the period, so the text doesn't need to end with one as well.
            if (custom && !isLast && separator.TrimStart().StartsWith('.'))
            {
                text = text.TrimEnd('.');
            }

            sb.Append(text);
            if (!isLast)
            {
                if (items[i + 1].Breaks > 0)
                {
                    // The line breaks replace the separator, keeping any punctuation it starts with.
                    sb.Append(separator.TrimEnd());
                    sb.Append('\n', items[i + 1].Breaks);
                }
                else
                {
                    sb.Append(separator);
                }
            }
            else if (terminate && (separator.EndsWith('\n') || separator.EndsWith('\r')))
            {
                var ending = separator.TrimEnd();
                if (ending.Length > 0 && !text.EndsWith(ending, StringComparison.Ordinal))
                {
                    sb.Append(ending);
                }
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
    /// their spacing is not stripped. Line breaks at the very end are always removed, since nothing follows them.
    /// </summary>
    /// <param name="combined">The combined string.</param>
    /// <param name="custom">Whether custom separators are in use.</param>
    /// <returns>The cleaned string.</returns>
    public static string Finish(string combined, bool custom)
    {
        return (custom ? combined : CleanUp(combined)).TrimEnd('\r', '\n');
    }

    private static string? SeparatorFor(StaticDefinition definition, string? stationSeparator)
    {
        return string.IsNullOrEmpty(definition.SeparatorAfter) ? stationSeparator : definition.SeparatorAfter;
    }

    private static string CleanUp(string text)
    {
        text = Regex.Replace(text, @"[!?.]*([!?.])", "$1");
        return Regex.Replace(text, "\\s+([.,!\":])", "$1");
    }
}
