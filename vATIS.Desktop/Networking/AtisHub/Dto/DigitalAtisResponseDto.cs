// <copyright file="DigitalAtisResponseDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Networking.AtisHub.Dto;

/// <summary>
/// Represents the response DTO for a digital ATIS request.
/// </summary>
public class DigitalAtisResponseDto
{
    /// <summary>
    /// Gets or sets the airport identifier.
    /// </summary>
    [JsonPropertyName("airport")]
    public string Airport { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of ATIS.
    /// </summary>
    [JsonPropertyName("type")]
    public string AtisType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ATIS letter code.
    /// </summary>
    [JsonPropertyName("code")]
    public string AtisLetter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full ATIS message body.
    /// </summary>
    [JsonPropertyName("datis")]
    public string Body { get; set; } = string.Empty;
}
