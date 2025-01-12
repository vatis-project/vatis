// <copyright file="AtisHubDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub.Dto;

/// <summary>
/// Represents a Data Transfer Object (DTO) for ATIS Hub.
/// </summary>
public class AtisHubDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AtisHubDto"/> class.
    /// </summary>
    /// <param name="stationId">The identifier of the station.</param>
    /// <param name="atisType">The type of ATIS.</param>
    /// <param name="atisLetter">The letter associated with the ATIS.</param>
    /// <param name="metar">The METAR (Meteorological Aerodrome Report) information.</param>
    /// <param name="wind">The wind information.</param>
    /// <param name="altimeter">The altimeter setting.</param>
    public AtisHubDto(string stationId, AtisType atisType, char atisLetter, string? metar, string? wind,
        string? altimeter)
    {
        StationId = stationId;
        AtisType = atisType;
        AtisLetter = atisLetter;
        Metar = metar;
        Wind = wind;
        Altimeter = altimeter;
    }

    /// <summary>
    /// Gets or sets the identifier of the station.
    /// </summary>
    public string? StationId { get; set; }

    /// <summary>
    /// Gets or sets the type of ATIS.
    /// </summary>
    public AtisType AtisType { get; set; }

    /// <summary>
    /// Gets or sets the letter associated with the ATIS.
    /// </summary>
    public char AtisLetter { get; set; }

    /// <summary>
    /// Gets or sets the METAR (Meteorological Aerodrome Report) information.
    /// </summary>
    public string? Metar { get; set; }

    /// <summary>
    /// Gets or sets the wind information.
    /// </summary>
    public string? Wind { get; set; }

    /// <summary>
    /// Gets or sets the altimeter setting.
    /// </summary>
    public string? Altimeter { get; set; }
}
