// <copyright file="IAtisHubConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;

namespace Vatsim.Vatis.Networking.AtisHub;

/// <summary>
/// Represents a connection to the ATIS hub.
/// </summary>
public interface IAtisHubConnection
{
    /// <summary>
    /// Connects to the ATIS hub.
    /// </summary>
    /// <returns>A task that represents the asynchronous connect operation.</returns>
    Task Connect();

    /// <summary>
    /// Disconnects from the ATIS hub.
    /// </summary>
    /// <returns>A task that represents the asynchronous disconnect operation.</returns>
    Task Disconnect();

    /// <summary>
    /// Publishes ATIS information to the hub.
    /// </summary>
    /// <param name="dto">The data transfer object containing ATIS information.</param>
    /// <returns>A task that represents the asynchronous publish operation.</returns>
    Task PublishAtis(AtisHubDto dto);

    /// <summary>
    /// Subscribes to ATIS information from the hub.
    /// </summary>
    /// <param name="dto">The data transfer object containing subscription information.</param>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
    Task SubscribeToAtis(SubscribeDto dto);
}
