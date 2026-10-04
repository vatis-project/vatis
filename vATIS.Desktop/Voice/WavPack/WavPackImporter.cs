// <copyright file="WavPackImporter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Converts a legacy <c>ATIS.txt</c> voice pack config (RECORD:text:file.wav lines)
/// into a vATIS <c>manifest.json</c>.
/// </summary>
public static class WavPackImporter
{
    private static readonly string[] s_digits =
        ["ZERO", "ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX", "SEVEN", "EIGHT", "NINER"];

    private static readonly string[] s_letters =
    [
        "ALPHA", "BRAVO", "CHARLIE", "DELTA", "ECHO", "FOXTROT", "GOLF", "HOTEL", "INDIA", "JULIET", "KILO", "LIMA",
        "MIKE", "NOVEMBER", "OSCAR", "PAPA", "QUEBEC", "ROMEO", "SIERRA", "TANGO", "UNIFORM", "VICTOR", "WHISKEY",
        "X-RAY", "YANKEE", "ZULU"
    ];

    /// <summary>
    /// Reads <c>ATIS.txt</c> in the folder and writes <c>manifest.json</c> next to it.
    /// </summary>
    /// <param name="packFolder">The folder containing ATIS.txt and the WAV files.</param>
    /// <returns>A report of what was imported and what was skipped.</returns>
    /// <exception cref="WavPackException">ATIS.txt was not found.</exception>
    public static WavPackImportReport Import(string packFolder)
    {
        var (manifest, report) = Convert(packFolder);
        WavPackManifestStore.Save(packFolder, manifest);
        return report;
    }

    /// <summary>
    /// Converts <c>ATIS.txt</c> in the folder into a manifest without writing anything.
    /// </summary>
    /// <param name="packFolder">The folder containing ATIS.txt and the WAV files.</param>
    /// <returns>The manifest and a report of what was imported and what was skipped.</returns>
    /// <exception cref="WavPackException">ATIS.txt was not found.</exception>
    public static (WavPackManifest Manifest, WavPackImportReport Report) Convert(string packFolder)
    {
        var atisTxt = Directory.EnumerateFiles(packFolder)
            .FirstOrDefault(f => string.Equals(Path.GetFileName(f), "ATIS.txt", StringComparison.OrdinalIgnoreCase))
            ?? throw new WavPackException($"ATIS.txt not found in {packFolder}");

        var files = Directory.EnumerateFiles(packFolder, "*.wav")
            .Select(Path.GetFileName)
            .OfType<string>()
            .GroupBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var manifest = new WavPackManifest { Name = new DirectoryInfo(packFolder).Name };
        var skipped = new List<string>();
        var duplicates = new List<string>();

        foreach (var line in File.ReadLines(atisTxt))
        {
            if (!line.StartsWith("RECORD:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var body = line["RECORD:".Length..].TrimEnd('\r');
            var split = body.LastIndexOf(':');
            if (split <= 0)
            {
                skipped.Add($"{body} (malformed)");
                continue;
            }

            var key = body[..split];
            var file = body[(split + 1)..].Trim();

            if (!files.TryGetValue(file, out var actualFile))
            {
                skipped.Add($"{key} (file not found: {file})");
                continue;
            }

            if (key == ",")
            {
                SetPauseFromClip(manifest, packFolder, actualFile);
                continue;
            }

            var tokens = MapKey(key);
            if (tokens.Count == 0)
            {
                skipped.Add($"{key} (no vATIS equivalent)");
                continue;
            }

            // vATIS speaks US spellings ("kilometers"); add them next to the UK spellings in the legacy pack.
            tokens.AddRange(tokens.Where(t => t.Contains("METRE", StringComparison.Ordinal))
                .Select(t => t.Replace("METRE", "METER", StringComparison.Ordinal)).ToList());

            foreach (var token in tokens)
            {
                if (!manifest.Clips.TryAdd(token, actualFile))
                {
                    duplicates.Add(token);
                }
            }
        }

        return (manifest, new WavPackImportReport(manifest.Clips.Count, skipped, duplicates));
    }

    private static List<string> MapKey(string key)
    {
        var comma = key.EndsWith(',') && key.Length > 1;
        var core = comma ? key[..^1] : key;
        var suffix = comma ? "," : string.Empty;

        // Legacy "X_" letter variants have no vATIS equivalent.
        if (core.EndsWith('_'))
        {
            return [];
        }

        if (core.Length == 1 && core[0] is >= '0' and <= '9')
        {
            var word = s_digits[core[0] - '0'];
            return core[0] == '9' ? [word + suffix, "NINE" + suffix] : [word + suffix];
        }

        if (core.Length == 1 && char.IsAsciiLetter(core[0]))
        {
            return [s_letters[char.ToUpperInvariant(core[0]) - 'A'] + suffix];
        }

        return core switch
        {
            "000" => ["THOUSAND" + suffix],
            "." => ["DECIMAL" + suffix, "POINT" + suffix],
            "-" => ["MINUS" + suffix],
            "+" => ["PLUS" + suffix],
            "10km or more" => ["10KM OR MORE" + suffix, "TEN KILOMETRES OR MORE" + suffix, "TEN KILOMETERS OR MORE" + suffix],
            _ => [core.ToUpperInvariant() + suffix]
        };
    }

    private static void SetPauseFromClip(WavPackManifest manifest, string folder, string file)
    {
        try
        {
            var samples = WavDecoder.Decode(File.ReadAllBytes(Path.Combine(folder, file)));
            manifest.PauseMs[","] = samples.Length * 1000 / WavDecoder.TargetSampleRate;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            // keep the default comma pause
        }
    }
}
