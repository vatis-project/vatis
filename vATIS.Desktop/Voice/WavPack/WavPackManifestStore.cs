// <copyright file="WavPackManifestStore.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.IO;
using System.Text.Json;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Reads and writes a voice pack's <c>manifest.json</c>.
/// </summary>
public static class WavPackManifestStore
{
    /// <summary>
    /// The manifest file name inside a pack folder.
    /// </summary>
    public const string FileName = "manifest.json";

    /// <summary>
    /// Loads the manifest in the pack folder.
    /// </summary>
    /// <param name="packFolder">The pack folder.</param>
    /// <returns>The manifest.</returns>
    /// <exception cref="WavPackException">The manifest is missing or invalid.</exception>
    public static WavPackManifest Load(string packFolder)
    {
        var path = Path.Combine(packFolder, FileName);
        if (!File.Exists(path))
        {
            throw new WavPackException($"Voice pack manifest not found: {path}");
        }

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(path), WavPackJsonContext.Default.WavPackManifest)
                   ?? throw new WavPackException("Voice pack manifest is empty.");
        }
        catch (JsonException ex)
        {
            throw new WavPackException($"Voice pack manifest is invalid: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes the manifest to the pack folder.
    /// </summary>
    /// <param name="packFolder">The pack folder.</param>
    /// <param name="manifest">The manifest to write.</param>
    public static void Save(string packFolder, WavPackManifest manifest)
    {
        File.WriteAllText(Path.Combine(packFolder, FileName),
            JsonSerializer.Serialize(manifest, WavPackJsonContext.Default.WavPackManifest));
    }
}
