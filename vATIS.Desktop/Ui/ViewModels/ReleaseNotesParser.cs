// <copyright file="ReleaseNotesParser.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Splits release notes Markdown (a version heading, <c>###</c> section headings and bullet lists) into sections.
/// </summary>
public static partial class ReleaseNotesParser
{
    private const string DefaultSectionTitle = "Notes";

    /// <summary>
    /// Parses the release notes Markdown.
    /// </summary>
    /// <param name="markdown">The Markdown text.</param>
    /// <returns>The parsed release notes.</returns>
    public static ParsedReleaseNotes Parse(string? markdown)
    {
        string? version = null;
        var sections = new List<(string Title, List<string> Items)>();

        foreach (var rawLine in (markdown ?? string.Empty).Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("###", StringComparison.Ordinal))
            {
                sections.Add((line.TrimStart('#').Trim(), []));
            }
            else if (line.StartsWith('#'))
            {
                version ??= line.TrimStart('#').Trim().TrimStart('v', 'V');
            }
            else
            {
                if (sections.Count == 0)
                    sections.Add((DefaultSectionTitle, []));

                var item = BulletRegex().Replace(line, string.Empty);
                if (item.Length > 0)
                    sections[^1].Items.Add(Clean(item));
            }
        }

        return new ParsedReleaseNotes(
            version,
            sections.Where(s => s.Items.Count > 0).Select(s => new ReleaseNoteSection(s.Title, s.Items)).ToList());
    }

    [GeneratedRegex(@"^[*+-]\s+")]
    private static partial Regex BulletRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex BoldRegex();

    private static string Clean(string item)
    {
        return BoldRegex().Replace(LinkRegex().Replace(item, "$1"), "$1");
    }
}
