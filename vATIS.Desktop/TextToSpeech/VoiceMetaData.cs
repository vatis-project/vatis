// <copyright file="VoiceMetaData.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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
