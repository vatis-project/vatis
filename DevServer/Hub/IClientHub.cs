// <copyright file="IClientHub.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using DevServer.Hub.Dto;

namespace DevServer.Hub;

/// <summary>
/// Represents an interface for a SignalR hub for client-server communication.
/// </summary>
public interface IClientHub
{
    /// <summary>
    /// Subscribes a client to ATIS updates.
    /// </summary>
    /// <param name="dto">The subscription data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AtisReceived(List<AtisHubDto> dto);

    /// <summary>
    /// Unsubscribes a client from ATIS updates.
    /// </summary>
    /// <param name="hubDto">The subscription data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAtisReceived(AtisHubDto hubDto);

    /// <summary>
    /// Sends METAR data to a client.
    /// </summary>
    /// <param name="metar">The METAR data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MetarReceived(string metar);
}
