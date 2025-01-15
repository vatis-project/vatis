// <copyright file="INavDataRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Vatsim.Vatis.NavData;

/// <summary>
/// Interface for navigation data repository.
/// </summary>
public interface INavDataRepository
{
    /// <summary>
    /// Initializes the navigation data repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Initialize();

    /// <summary>
    /// Checks for updates in the navigation data repository.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CheckForUpdates();

    /// <summary>
    /// Gets the airport information by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the airport.</param>
    /// <returns>The airport information if found; otherwise, null.</returns>
    Airport? GetAirport(string id);

    /// <summary>
    /// Gets the navaid information by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the navaid.</param>
    /// <returns>The navaid information if found; otherwise, null.</returns>
    Navaid? GetNavaid(string id);
}
