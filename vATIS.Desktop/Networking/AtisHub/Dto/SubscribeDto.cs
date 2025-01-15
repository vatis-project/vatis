// <copyright file="SubscribeDto.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub.Dto;

/// <summary>
/// Represents a data transfer object for subscribing to ATIS (Automatic Terminal Information Service) updates.
/// </summary>
public class SubscribeDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubscribeDto"/> class.
    /// </summary>
    /// <param name="stationId">The identifier of the station to subscribe to.</param>
    /// <param name="atisType">The type of ATIS information to subscribe to.</param>
    public SubscribeDto(string stationId, AtisType atisType)
    {
        StationId = stationId;
        AtisType = atisType;
    }

    /// <summary>
    /// Gets or sets the identifier of the station to subscribe to.
    /// </summary>
    public string StationId { get; set; }

    /// <summary>
    /// Gets or sets the type of ATIS information to subscribe to.
    /// </summary>
    public AtisType AtisType { get; set; }
}
