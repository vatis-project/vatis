// <copyright file="WavPackManifest.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Voice.WavPack;

/// <summary>
/// Describes a voice pack: a mapping of spoken tokens to WAV files in the pack folder.
/// </summary>
public class WavPackManifest
{
    /// <summary>
    /// Gets or sets the display name of the pack.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a hash of the pack's clips and settings, used to recognise an identical pack when it is imported again.
    /// </summary>
    [JsonPropertyName("contentHash")]
    public string? ContentHash { get; set; }

    /// <summary>
    /// Gets or sets the silence, in milliseconds, inserted between words.
    /// </summary>
    [JsonPropertyName("gapMs")]
    public int GapMs { get; set; } = 60;

    /// <summary>
    /// Gets or sets the silence, in milliseconds, inserted for punctuation (",", ".", ":", "!", "?").
    /// </summary>
    [JsonPropertyName("pauseMs")]
    public Dictionary<string, int> PauseMs { get; set; } = new()
    {
        [","] = 250, ["."] = 500, [":"] = 250, ["!"] = 500, ["?"] = 500
    };

    /// <summary>
    /// Gets or sets the mapping of spoken token (e.g. "ALFA", "ONE") to a WAV path relative to the pack folder.
    /// </summary>
    [JsonPropertyName("clips")]
    public Dictionary<string, string> Clips { get; set; } = new();
}
