// <copyright file="WavPackLibrary.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// vATIS's library of voice packs. Each pack lives in its own folder, named by the pack's id, under the app's voice pack
/// folder, and any number of stations can use the same pack.
/// </summary>
public static class WavPackLibrary
{
    /// <summary>
    /// Finds the folder of the voice pack a station uses.
    /// </summary>
    /// <param name="voice">The station's voice settings.</param>
    /// <returns>The pack folder, or null if the station has no pack or it can't be found.</returns>
    public static string? ResolveFolder(AtisVoiceMeta voice)
    {
        if (!string.IsNullOrWhiteSpace(voice.WavPackId))
        {
            return FolderForId(voice.WavPackId);
        }

        // packs configured by folder path before the library existed
        return !string.IsNullOrWhiteSpace(voice.WavPackPath) && Directory.Exists(voice.WavPackPath)
            ? voice.WavPackPath
            : null;
    }

    /// <summary>
    /// Gets the folder of a library pack.
    /// </summary>
    /// <param name="id">The pack id.</param>
    /// <returns>The folder, or null if there is no such pack.</returns>
    public static string? FolderForId(string id)
    {
        if (id.Length == 0 || id.StartsWith('.') || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        var folder = Path.Combine(PathProvider.VoicePacksFolderPath, id);
        return File.Exists(Path.Combine(folder, WavPackManifestStore.FileName)) ? folder : null;
    }

    /// <summary>
    /// Lists the voice packs in the library.
    /// </summary>
    /// <returns>The packs, ordered by name.</returns>
    public static IReadOnlyList<WavPackInfo> List()
    {
        var root = PathProvider.VoicePacksFolderPath;
        if (!Directory.Exists(root))
        {
            return [];
        }

        var packs = new List<(string Id, string Folder, string Name, int Count)>();
        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            var id = Path.GetFileName(folder);
            if (id.StartsWith('.'))
            {
                continue; // temporary import folders
            }

            try
            {
                var manifest = WavPackManifestStore.Load(folder);
                packs.Add((id, folder, string.IsNullOrWhiteSpace(manifest.Name) ? id : manifest.Name,
                    manifest.Clips.Count));
            }
            catch (WavPackException)
            {
                // not a voice pack
            }
        }

        return packs
            .Select(p => new WavPackInfo(p.Id, p.Folder, p.Name, p.Count,
                packs.Count(x => string.Equals(x.Name, p.Name, StringComparison.OrdinalIgnoreCase)) > 1
                    ? $"{p.Name} ({p.Id})"
                    : p.Name))
            .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Copies a voice pack folder into the library. The folder must contain either a <c>manifest.json</c> or a legacy
    /// <c>ATIS.txt</c> (which is converted); the source folder is never modified. Only the WAV files the manifest
    /// references are copied. Importing a pack identical to one already in the library reuses that pack.
    /// </summary>
    /// <param name="sourceFolder">The folder to import.</param>
    /// <returns>
    /// The pack, the conversion report for a legacy pack, and whether an identical pack was already in the library
    /// (in which case nothing is copied).
    /// </returns>
    /// <exception cref="WavPackException">The folder is not a valid voice pack.</exception>
    public static (WavPackInfo Pack, WavPackImportReport? Report, bool Reused) Import(string sourceFolder)
    {
        if (!Directory.Exists(sourceFolder))
        {
            throw new WavPackException($"Folder not found: {sourceFolder}");
        }

        WavPackManifest manifest;
        WavPackImportReport? report = null;
        var manifestPath = Path.Combine(sourceFolder, WavPackManifestStore.FileName);

        if (File.Exists(manifestPath))
        {
            manifest = WavPackManifestStore.Load(sourceFolder);
        }
        else if (Directory.EnumerateFiles(sourceFolder)
                 .Any(f => string.Equals(Path.GetFileName(f), "ATIS.txt", StringComparison.OrdinalIgnoreCase)))
        {
            (manifest, report) = WavPackImporter.Convert(sourceFolder);
        }
        else
        {
            throw new WavPackException("The folder contains neither manifest.json nor ATIS.txt.");
        }

        var sourceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceFolder));
        var files = manifest.Clips.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var relative in files)
        {
            var full = Path.GetFullPath(Path.Combine(sourceRoot, relative));
            if (!full.StartsWith(sourceRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                !File.Exists(full))
            {
                throw new WavPackException($"Clip file not found: {relative}");
            }
        }

        var hash = ComputeContentHash(manifest, sourceRoot);
        var existing = List().FirstOrDefault(p => StoredOrComputedHash(p.Folder) == hash);
        if (existing != null)
        {
            return (existing, report, true);
        }

        var packs = List();
        var baseName = string.IsNullOrWhiteSpace(manifest.Name) ? new DirectoryInfo(sourceRoot).Name : manifest.Name;
        var name = baseName;
        for (var i = 2; packs.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)); i++)
        {
            name = $"{baseName} ({i})";
        }

        manifest.Name = name;
        manifest.ContentHash = hash;

        var id = Guid.NewGuid().ToString("N");
        var target = Path.Combine(PathProvider.VoicePacksFolderPath, id);
        try
        {
            Directory.CreateDirectory(target);
            foreach (var relative in files)
            {
                var destination = Path.Combine(target, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(Path.Combine(sourceRoot, relative), destination, true);
            }

            WavPackManifestStore.Save(target, manifest);
        }
        catch
        {
            if (Directory.Exists(target))
            {
                Directory.Delete(target, true);
            }

            throw;
        }

        return (new WavPackInfo(id, target, name, manifest.Clips.Count, name), report, false);
    }

    /// <summary>
    /// Renames a library pack.
    /// </summary>
    /// <param name="id">The pack id.</param>
    /// <param name="newName">The new name.</param>
    /// <exception cref="WavPackException">The name is empty or taken, or the pack doesn't exist.</exception>
    public static void Rename(string id, string newName)
    {
        newName = newName.Trim();
        if (newName.Length == 0)
        {
            throw new WavPackException("Enter a name for the voice pack.");
        }

        var folder = FolderForId(id) ?? throw new WavPackException("The voice pack was not found.");
        if (List().Any(p => p.Id != id && string.Equals(p.Name, newName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new WavPackException($"Another voice pack is already named \"{newName}\".");
        }

        var manifest = WavPackManifestStore.Load(folder);
        manifest.Name = newName;
        WavPackManifestStore.Save(folder, manifest);
    }

    /// <summary>
    /// Deletes a library pack and its files.
    /// </summary>
    /// <param name="id">The pack id.</param>
    public static void Delete(string id)
    {
        var folder = FolderForId(id);
        if (folder != null)
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>
    /// Recalculates and stores the content hash of a pack after its clips have been edited, so it is no longer
    /// mistaken for the pack it was imported from.
    /// </summary>
    /// <param name="folder">The pack folder.</param>
    public static void StampContentHash(string folder)
    {
        var manifest = WavPackManifestStore.Load(folder);
        manifest.ContentHash = ComputeContentHash(manifest, folder);
        WavPackManifestStore.Save(folder, manifest);
    }

    private static string? StoredOrComputedHash(string folder)
    {
        try
        {
            var manifest = WavPackManifestStore.Load(folder);
            return string.IsNullOrEmpty(manifest.ContentHash)
                ? ComputeContentHash(manifest, folder)
                : manifest.ContentHash;
        }
        catch (Exception ex) when (ex is WavPackException or IOException)
        {
            return null;
        }
    }

    private static string ComputeContentHash(WavPackManifest manifest, string root)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        void Append(string text) => hash.AppendData(Encoding.UTF8.GetBytes(text + "\n"));

        Append($"gap={manifest.GapMs}");
        foreach (var (key, ms) in manifest.PauseMs.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            Append($"pause:{key}={ms}");
        }

        foreach (var (key, file) in manifest.Clips.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            Append($"clip:{key}={file}");
        }

        foreach (var file in manifest.Clips.Values.Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(x => x, StringComparer.Ordinal))
        {
            hash.AppendData(File.ReadAllBytes(Path.Combine(root, file)));
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
}
