// <copyright file="WavPackService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Stitches pre-recorded WAV clips into a single PCM buffer.
/// </summary>
public class WavPackService : IWavPackService
{
    private const int FrameSamples = 960; // 20 ms at 48 kHz, the Opus frame size used by the bot encoder
    private const string ManifestFileName = "manifest.json";
    private static readonly char[] s_pauseChars = [',', '.', ':', '!', '?'];

    private readonly ConcurrentDictionary<string, (DateTime Stamp, short[] Samples)> _clipCache =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public WavPackResult Build(string spokenText, string packFolder)
    {
        var manifest = LoadManifest(packFolder);

        // Keys ending in "," are variants used when the phrase is followed by a comma (falling intonation).
        var plain = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var beforeComma = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var maxWords = 1;
        foreach (var (key, file) in manifest.Clips)
        {
            var normalized = Normalize(key);
            if (normalized.Length == 0)
            {
                continue;
            }

            (key.TrimEnd().EndsWith(',') ? beforeComma : plain)[normalized] = file;
            maxWords = Math.Max(maxWords, normalized.Split(' ').Length);
        }

        var tokens = new List<(string Word, List<string> Pauses)>();
        foreach (var raw in spokenText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var (word, pauses) = SplitPunctuation(raw);
            if (word.Length == 0 && tokens.Count > 0)
            {
                tokens[^1].Pauses.AddRange(pauses);
            }
            else
            {
                tokens.Add((word, pauses));
            }
        }

        var missing = new List<string>();
        var pieces = new List<short[]>();
        var gap = Silence(manifest.GapMs);

        var i = 0;
        while (i < tokens.Count)
        {
            var matched = 0;
            for (var len = Math.Min(maxWords, tokens.Count - i); len >= 1 && matched == 0; len--)
            {
                var phrase = Normalize(string.Join(' ', tokens.Skip(i).Take(len).Select(t => t.Word)));
                var last = tokens[i + len - 1];
                string? file = null;
                if (last.Pauses.Contains(",") && beforeComma.TryGetValue(phrase, out var variant))
                {
                    file = variant;
                }
                else if (plain.TryGetValue(phrase, out var normal))
                {
                    file = normal;
                }

                if (file != null)
                {
                    pieces.Add(LoadClip(packFolder, file));
                    pieces.Add(gap);
                    matched = len;
                }
            }

            if (matched == 0)
            {
                if (!missing.Contains(tokens[i].Word, StringComparer.OrdinalIgnoreCase))
                {
                    missing.Add(tokens[i].Word);
                }

                matched = 1;
            }

            foreach (var p in tokens[i + matched - 1].Pauses)
            {
                if (manifest.PauseMs.TryGetValue(p, out var ms))
                {
                    pieces.Add(Silence(ms));
                }
            }

            i += matched;
        }

        if (missing.Count > 0)
        {
            return new WavPackResult(null, missing);
        }

        var total = pieces.Sum(p => p.Length);
        total -= total % FrameSamples;
        var pcm = new byte[total * 2];
        var offset = 0;
        foreach (var piece in pieces)
        {
            var take = Math.Min(piece.Length, total - offset);
            if (take <= 0)
            {
                break;
            }

            Buffer.BlockCopy(piece, 0, pcm, offset * 2, take * 2);
            offset += take;
        }

        return new WavPackResult(pcm, missing);
    }

    /// <inheritdoc/>
    public (int ClipCount, IReadOnlyList<string> Problems) Validate(string packFolder)
    {
        var problems = new List<string>();
        WavPackManifest manifest;
        try
        {
            manifest = LoadManifest(packFolder);
        }
        catch (WavPackException ex)
        {
            return (0, [ex.Message]);
        }

        foreach (var (token, relative) in manifest.Clips)
        {
            try
            {
                LoadClip(packFolder, relative);
            }
            catch (Exception ex) when (ex is WavPackException or InvalidOperationException or IOException)
            {
                problems.Add($"{token}: {ex.Message}");
            }
        }

        return (manifest.Clips.Count, problems);
    }

    private static string Normalize(string phrase)
    {
        var chars = phrase.Where(c => Array.IndexOf(s_pauseChars, c) < 0).ToArray();
        return string.Join(' ', new string(chars).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private static (string Word, List<string> Pauses) SplitPunctuation(string token)
    {
        var pauses = new List<string>();
        var end = token.Length;
        while (end > 0 && Array.IndexOf(s_pauseChars, token[end - 1]) >= 0)
        {
            pauses.Insert(0, token[end - 1].ToString());
            end--;
        }

        return (token[..end], pauses);
    }

    private static short[] Silence(int ms) => new short[Math.Max(0, ms) * WavDecoder.TargetSampleRate / 1000];

    private static WavPackManifest LoadManifest(string packFolder) => WavPackManifestStore.Load(packFolder);

    private short[] LoadClip(string packFolder, string relative)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(packFolder));
        var full = Path.GetFullPath(Path.Combine(root, relative));
        if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new WavPackException($"Clip path escapes the pack folder: {relative}");
        }

        if (!File.Exists(full))
        {
            throw new WavPackException($"Clip file not found: {relative}");
        }

        var stamp = File.GetLastWriteTimeUtc(full);
        if (_clipCache.TryGetValue(full, out var cached) && cached.Stamp == stamp)
        {
            return cached.Samples;
        }

        var samples = WavDecoder.Decode(File.ReadAllBytes(full));
        _clipCache[full] = (stamp, samples);
        return samples;
    }
}
