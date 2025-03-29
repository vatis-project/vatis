// <copyright file="IVoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Network;

/// <summary>
/// Defines the contract for managing connections to a voice server, including authentication and
/// bot management functionality.
/// </summary>
public interface IVoiceServerConnection
{
    /// <summary>
    /// Establishes a connection to the voice server.
    /// </summary>
    /// <returns>A task that represents the asynchronous connection operation.</returns>
    Task Connect();

    /// <summary>
    /// Terminates the current connection to the voice server and clears any associated session state.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Adds a new bot or updates an existing bot on the voice server with the provided callsign
    /// and configuration details.
    /// </summary>
    /// <param name="callsign">The callsign of the bot to be added or updated.</param>
    /// <param name="dto">The details of the bot, including transceivers and audio settings.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the bot associated with the specified callsign from the voice server.
    /// </summary>
    /// <param name="callsign">The callsign of the bot to be removed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveBot(string callsign, CancellationToken? cancellationToken = null);
}
