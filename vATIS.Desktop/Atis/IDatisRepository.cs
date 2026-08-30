// <copyright file="IDatisRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Manages periodic fetching of D-ATIS data for monitored stations.
/// </summary>
public interface IDatisRepository
{
    /// <summary>
    /// Begins monitoring a station for D-ATIS data, fetching immediately and then on each timer tick.
    /// </summary>
    /// <param name="station">The ATIS station to monitor.</param>
    void MonitorStation(AtisStation station);

    /// <summary>
    /// Stops monitoring a station for D-ATIS data.
    /// </summary>
    /// <param name="stationId">The unique identifier of the station to remove.</param>
    void RemoveStation(string stationId);
}
