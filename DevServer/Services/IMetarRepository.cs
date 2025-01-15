// <copyright file="IMetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace DevServer.Services;

/// <summary>
/// Provides an interface for retrieving METAR data from the VATSIM METAR endpoint.
/// </summary>
public interface IMetarRepository
{
    /// <summary>
    /// Gets the METAR data for the specified ICAO airport identifier.
    /// </summary>
    /// <param name="id">The ICAO airport identifier.</param>
    /// <returns>The METAR data for the specified airport identifier.</returns>
    Task<string?> GetVatsimMetar(string id);
}
