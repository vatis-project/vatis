// <copyright file="IMetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

/// <summary>
/// Defines methods for retrieving and managing decoded METAR data from weather stations.
/// </summary>
public interface IMetarRepository
{
    /// <summary>
    /// Retrieves a decoded METAR report for a specific station.
    /// </summary>
    /// <param name="station">
    /// The ICAO identifier of the station for which the METAR report is requested.
    /// </param>
    /// <param name="monitor">
    /// A boolean value that specifies whether to monitor the station (optional, default is false).
    /// </param>
    /// <param name="triggerMessageBus">
    /// A boolean value that specifies whether to trigger the message bus for updates (optional, default is true).
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation, containing a <see cref="DecodedMetar"/>
    /// object if the METAR report is successfully retrieved, or null if the operation fails.
    /// </returns>
    Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true);

    /// <summary>
    /// Removes the METAR data associated with the specified station.
    /// </summary>
    /// <param name="station">
    /// The ICAO identifier of the station for which the METAR data should be removed.
    /// </param>
    void RemoveMetar(string station);
}
