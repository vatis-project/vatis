// <copyright file="VoiceMetaData.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.TextToSpeech;

/// <summary>
/// Represents metadata about a specific voice in the text-to-speech system.
/// </summary>
public class VoiceMetaData
{
    /// <summary>
    /// Gets or sets the name of the voice.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the voice.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}

/// <summary>
/// Extensions for looking up voices.
/// </summary>
public static class VoiceMetaDataExtensions
{
    private static readonly Dictionary<string, string> LegacyVoiceIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Default"] = "default",
        ["US Male"] = "us-male",
        ["US Female"] = "us-female",
        ["UK Male"] = "uk-male",
        ["UK Female"] = "uk-female",
        ["AU Male"] = "au-male",
        ["AU Female"] = "au-female",
    };

    /// <summary>
    /// Converts a voice setting saved by older versions, which stored the voice's display name, to its id. Anything
    /// else (an id, or a name this version does not know) is returned unchanged.
    /// </summary>
    /// <param name="saved">The saved voice setting.</param>
    /// <returns>The voice id, or <paramref name="saved"/> when it is not a known legacy name.</returns>
    public static string? ConvertLegacyVoiceName(string? saved) =>
        saved != null && LegacyVoiceIds.TryGetValue(saved.Trim(), out var id) ? id : saved;

    /// <summary>
    /// Resolves a saved voice setting to a voice id. Profiles now store the id; older ones stored the display name
    /// (e.g. <c>US Male</c>, which the hub now reports as <c>US Male (Nathan)</c>), so a name, or the start of one,
    /// is accepted too.
    /// </summary>
    /// <param name="voices">The voices reported by the hub.</param>
    /// <param name="saved">The saved voice id or legacy name.</param>
    /// <returns>The matching voice id, or <c>default</c> when nothing matches.</returns>
    public static string ResolveVoiceId(this IEnumerable<VoiceMetaData> voices, string? saved)
    {
        if (string.IsNullOrWhiteSpace(saved))
        {
            return "default";
        }

        var list = voices as IReadOnlyCollection<VoiceMetaData> ?? voices.ToList();
        var match = list.FirstOrDefault(v => string.Equals(v.Id, saved, StringComparison.OrdinalIgnoreCase)) ??
                    list.FirstOrDefault(v => string.Equals(v.Name, saved, StringComparison.OrdinalIgnoreCase)) ??
                    list.FirstOrDefault(v => v.Name.StartsWith(saved + " (", StringComparison.OrdinalIgnoreCase));
        return match?.Id ?? "default";
    }
}
