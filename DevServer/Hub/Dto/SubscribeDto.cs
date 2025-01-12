// <copyright file="SubscribeDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using DevServer.Models;

namespace DevServer.Hub.Dto;

/// <summary>
/// Represents a DTO for subscribing to ATIS data.
/// </summary>
public class SubscribeDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubscribeDto"/> class.
    /// </summary>
    /// <param name="stationId">The station identifier.</param>
    /// <param name="atisType">The ATIS type.</param>
    public SubscribeDto(string stationId, AtisType atisType)
    {
        StationId = stationId;
        AtisType = atisType;
    }

    /// <summary>
    /// Gets or sets the station identifier.
    /// </summary>
    public string StationId { get; set; }

    /// <summary>
    /// Gets or sets the ATIS type.
    /// </summary>
    public AtisType AtisType { get; set; }
}
