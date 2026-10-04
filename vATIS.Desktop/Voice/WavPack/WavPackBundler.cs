// <copyright file="WavPackBundler.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Packs profiles and stations together with the WAV voice packs they use into a single zip, and unpacks them.
/// </summary>
/// <remarks>
/// Layout: <c>profile.json</c> or <c>station.json</c> at the root, and for each pack
/// <c>voicepacks/&lt;name&gt;/manifest.json</c> plus the WAV files the manifest references. Stations in the JSON refer to
/// their pack by <c>&lt;name&gt;</c>; on import each pack is added to the library (or matched with an identical one
/// already there) and the stations are pointed at it.
/// </remarks>
public static partial class WavPackBundler
{
    /// <summary>
    /// The entry name of the profile JSON in a bundle.
    /// </summary>
    public const string ProfileEntry = "profile.json";

    /// <summary>
    /// The entry name of the station JSON in a bundle.
    /// </summary>
    public const string StationEntry = "station.json";

    private const string PackPrefix = "voicepacks/";
    private const long MaxExtractedBytes = 500L * 1024 * 1024;
    private const int MaxEntries = 5000;

    /// <summary>
    /// Gets a value indicating whether any of the stations uses a WAV voice pack.
    /// </summary>
    /// <param name="stations">The stations to check.</param>
    /// <returns>True if at least one station uses a voice pack.</returns>
    public static bool UsesVoicePack(IEnumerable<AtisStation>? stations)
    {
        return stations != null && stations.Any(s => s.AtisVoice is { UseWavPack: true });
    }

    /// <summary>
    /// Creates a bundle zip. Voice pack folders are copied in and station paths in the JSON are made relative.
    /// </summary>
    /// <param name="zipPath">The zip to write.</param>
    /// <param name="mainEntryName">The JSON entry name (<see cref="ProfileEntry"/> or <see cref="StationEntry"/>).</param>
    /// <param name="stations">The stations of the cloned object that will be serialized; their paths are rewritten.</param>
    /// <param name="serializeMain">Serializes the (rewritten) cloned object.</param>
    /// <exception cref="WavPackException">A station's voice pack is missing or invalid.</exception>
    public static void CreateBundle(string zipPath, string mainEntryName, IReadOnlyList<AtisStation> stations,
        Func<string> serializeMain)
    {
        var packNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var station in stations)
        {
            var voice = station.AtisVoice;
            if (!voice.UseWavPack)
            {
                voice.WavPackId = null;
                voice.WavPackPath = null;
                continue;
            }

            var folder = WavPackLibrary.ResolveFolder(voice) ??
                         throw new WavPackException(
                             string.IsNullOrWhiteSpace(voice.WavPackId) && string.IsNullOrWhiteSpace(voice.WavPackPath)
                                 ? $"{station.Name} is set to use a WAV voice pack, but none is saved for it. Choose one on the General tab and click Apply."
                                 : $"The voice pack used by {station.Name} could not be found. It may have been deleted in Manage Voice Packs.");

            var key = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
            if (!packNames.TryGetValue(key, out var name))
            {
                string? manifestName = null;
                try
                {
                    manifestName = WavPackManifestStore.Load(key).Name;
                }
                catch (WavPackException)
                {
                    // reported below when the pack is added
                }

                name = UniqueName(SanitizeName(string.IsNullOrWhiteSpace(manifestName) ? new DirectoryInfo(key).Name : manifestName), usedNames);
                packNames[key] = name;
            }

            // inside a bundle a station refers to its pack by the pack's name in the zip
            voice.WavPackId = name;
            voice.WavPackPath = null;
        }

        var tempPath = zipPath + ".tmp";
        try
        {
            using (var zip = ZipFile.Open(tempPath, ZipArchiveMode.Create))
            {
                foreach (var (folder, name) in packNames)
                {
                    AddPack(zip, folder, name);
                }

                var entry = zip.CreateEntry(mainEntryName, CompressionLevel.Optimal);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
                writer.Write(serializeMain());
            }

            File.Move(tempPath, zipPath, true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Reads the JSON from a bundle and extracts its voice packs into the app's voice pack folder.
    /// </summary>
    /// <param name="zipPath">The bundle.</param>
    /// <param name="mainEntryName">The JSON entry name to read.</param>
    /// <returns>The JSON text and the map from pack name in the bundle to the id of the library pack.</returns>
    /// <exception cref="WavPackException">The bundle is invalid or unsafe.</exception>
    public static (string Json, IReadOnlyDictionary<string, string> PackIds) ReadBundle(string zipPath,
        string mainEntryName)
    {
        using var zip = ZipFile.OpenRead(zipPath);

        if (zip.Entries.Count > MaxEntries || zip.Entries.Sum(e => e.Length) > MaxExtractedBytes)
        {
            throw new WavPackException("The bundle is too large.");
        }

        var main = zip.GetEntry(mainEntryName)
                   ?? throw new WavPackException($"The bundle does not contain {mainEntryName}.");
        string json;
        using (var reader = new StreamReader(main.Open(), Encoding.UTF8))
        {
            json = reader.ReadToEnd();
        }

        var packs = zip.Entries
            .Where(e => e.FullName.StartsWith(PackPrefix, StringComparison.Ordinal) && e.Name.Length > 0)
            .GroupBy(e => e.FullName[PackPrefix.Length..].Split('/')[0]);

        var packIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pack in packs)
        {
            var manifestEntry = pack.FirstOrDefault(e => e.FullName == $"{PackPrefix}{pack.Key}/{WavPackManifestStore.FileName}")
                                ?? throw new WavPackException($"Voice pack {pack.Key} has no manifest.json.");

            var temp = Path.Combine(Path.GetTempPath(), "vatis-import-" + Guid.NewGuid().ToString("N"));
            try
            {
                Extract(pack, $"{PackPrefix}{pack.Key}/", temp, manifestEntry);
                packIds[pack.Key] = WavPackLibrary.Import(temp).Pack.Id!;
            }
            finally
            {
                if (Directory.Exists(temp))
                {
                    Directory.Delete(temp, true);
                }
            }
        }

        return (json, packIds);
    }

    /// <summary>
    /// Points the given stations at the library packs their bundle packs were imported as.
    /// </summary>
    /// <param name="stations">The imported stations.</param>
    /// <param name="packIds">The map returned by <see cref="ReadBundle"/>.</param>
    public static void ResolvePacks(IEnumerable<AtisStation> stations, IReadOnlyDictionary<string, string> packIds)
    {
        foreach (var voice in stations.Select(s => s.AtisVoice))
        {
            voice.WavPackPath = null;
            if (voice.WavPackId != null && packIds.TryGetValue(voice.WavPackId, out var id))
            {
                voice.WavPackId = id;
            }
            else
            {
                voice.WavPackId = null;
            }
        }
    }

    /// <summary>
    /// Makes a pack name safe to use as a folder name.
    /// </summary>
    /// <param name="name">The pack name.</param>
    /// <returns>The sanitized name.</returns>
    internal static string SanitizeName(string name)
    {
        var cleaned = InvalidNameChars().Replace(name, "_").Trim('.', ' ', '_');
        return cleaned.Length == 0 ? "pack" : cleaned;
    }

    private static void AddPack(ZipArchive zip, string folder, string name)
    {
        var manifest = WavPackManifestStore.Load(folder);
        var root = folder + Path.DirectorySeparatorChar;

        zip.CreateEntryFromFile(Path.Combine(folder, WavPackManifestStore.FileName),
            $"{PackPrefix}{name}/{WavPackManifestStore.FileName}", CompressionLevel.Optimal);

        foreach (var relative in manifest.Clips.Values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var full = Path.GetFullPath(Path.Combine(folder, relative));
            if (!full.StartsWith(root, StringComparison.Ordinal) || !File.Exists(full))
            {
                throw new WavPackException($"Clip file not found in {name}: {relative}");
            }

            zip.CreateEntryFromFile(full, $"{PackPrefix}{name}/{relative.Replace('\\', '/')}", CompressionLevel.Fastest);
        }
    }

    private static void Extract(IEnumerable<ZipArchiveEntry> entries, string prefix, string dest,
        ZipArchiveEntry manifestEntry)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dest)) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(dest);

        foreach (var entry in entries)
        {
            var relative = entry.FullName[prefix.Length..];
            var isManifest = entry == manifestEntry;
            if (!isManifest && !relative.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                continue; // only audio and the manifest are ever extracted
            }

            var target = Path.GetFullPath(Path.Combine(dest, relative));
            if (!target.StartsWith(root, StringComparison.Ordinal))
            {
                throw new WavPackException("The bundle contains an unsafe file path.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, true);
        }
    }

    private static string ContentHash(IEnumerable<ZipArchiveEntry> entries)
    {
        var text = string.Join('|', entries.OrderBy(e => e.FullName, StringComparer.Ordinal)
            .Select(e => $"{e.FullName}:{e.Length}:{e.Crc32}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..8].ToLowerInvariant();
    }

    private static string UniqueName(string name, HashSet<string> used)
    {
        var candidate = name;
        for (var i = 2; !used.Add(candidate); i++)
        {
            candidate = $"{name}-{i}";
        }

        return candidate;
    }

    [GeneratedRegex(@"[^A-Za-z0-9._ -]")]
    private static partial Regex InvalidNameChars();
}
