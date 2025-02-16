// <copyright file="TextToSpeechRequestDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.TextToSpeech;

/// <summary>
/// Represents a Data Transfer Object (DTO) for a text-to-speech request.
/// </summary>
public class TextToSpeechRequestDto
{
    /// <summary>
    /// Gets or sets the station identifier.
    /// </summary>
    public string? Station { get; set; }

    /// <summary>
    /// Gets or sets the text to be converted to speech.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the voice identifier for the text-to-speech request.
    /// </summary>
    public string? Voice { get; set; }

    /// <summary>
    /// Gets or sets the speech rate multiplier.
    /// </summary>
    public double? SpeechRate { get; set; }

    /// <summary>
    /// Gets or sets the JSON Web Token (JWT) used for authentication.
    /// </summary>
    public string? Jwt { get; set; }
}
