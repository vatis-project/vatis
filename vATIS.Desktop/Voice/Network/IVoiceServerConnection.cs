// <copyright file="IVoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Network;

/// <summary>
/// Represents a connection to a voice server that supports actions such as connecting, disconnecting,
/// and managing ATIS bots.
/// </summary>
public interface IVoiceServerConnection
{
    /// <summary>
    /// Connects to the voice server using the provided credentials.
    /// </summary>
    /// <param name="username">The username to authenticate with the voice server.</param>
    /// <param name="password">The password associated with the username.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task Connect(string username, string password);

    /// <summary>
    /// Disconnects from the voice server and cleans up any associated resources.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Adds or updates a bot on the voice server using the provided callsign and bot request data.
    /// </summary>
    /// <param name="callsign">The unique identifier for the bot being added or updated.</param>
    /// <param name="dto">The data transfer object containing the bot's configuration details.</param>
    /// <param name="cancellationToken">A token used to propagate notification that the operation should be canceled.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a bot from the voice server using the specified callsign.
    /// </summary>
    /// <param name="callsign">The unique callsign identifying the bot to be removed.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    Task RemoveBot(string callsign);
}
