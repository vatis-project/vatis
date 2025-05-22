// <copyright file="AtisHubDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using DevServer.Models;

namespace DevServer.Hub.Dto;

/// <summary>
/// Represents a DTO for ATIS data.
/// </summary>
public class AtisHubDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the ATIS is currently online.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets the station identifier.
    /// </summary>
    public string? StationId { get; set; }

    /// <summary>
    /// Gets or sets the ATIS type.
    /// </summary>
    public AtisType AtisType { get; set; }

    /// <summary>
    /// Gets or sets the ATIS letter.
    /// </summary>
    public char AtisLetter { get; set; }

    /// <summary>
    /// Gets or sets the METAR string.
    /// </summary>
    public string? Metar { get; set; }

    /// <summary>
    /// Gets or sets the wind data.
    /// </summary>
    public string? Wind { get; set; }

    /// <summary>
    /// Gets or sets the altimeter data.
    /// </summary>
    public string? Altimeter { get; set; }

    /// <summary>
    /// Gets or sets the airport conditions.
    /// </summary>
    public string? AirportConditions { get; set; }

    /// <summary>
    /// Gets or sets the NOTAMs.
    /// </summary>
    public string? Notams { get; set; }

    /// <summary>
    /// Gets or sets the complete text ATIS string.
    /// </summary>
    public string? TextAtis { get; set; }
}
