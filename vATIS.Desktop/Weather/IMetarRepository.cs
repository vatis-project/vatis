// <copyright file="IMetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

/// <summary>
/// Repository for METAR data.
/// </summary>
public interface IMetarRepository
{
    /// <summary>
    /// Gets the METAR data for the specified station.
    /// </summary>
    /// <param name="station">The station identifier.</param>
    /// <param name="monitor">Whether to monitor the station for updates.</param>
    /// <param name="triggerMessageBus">Whether to trigger the message bus.</param>
    /// <returns>The decoded METAR.</returns>
    Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true);

    /// <summary>
    /// Removes the METAR from being monitored.
    /// </summary>
    /// <param name="station">The station identifier.</param>
    void RemoveMetar(string station);
}
