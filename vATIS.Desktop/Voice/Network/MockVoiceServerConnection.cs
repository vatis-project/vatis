// <copyright file="MockVoiceServerConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Voice.Dto;

namespace Vatsim.Vatis.Voice.Network;

/// <summary>
/// Provides a mock implementation of the <see cref="MockVoiceServerConnection"/> class for simulating a voice server connection.
/// </summary>
public class MockVoiceServerConnection : IVoiceServerConnection
{
    /// <inheritdoc />
    public Task Connect(string username, string password)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        // Not implemented
    }

    /// <inheritdoc />
    public Task AddOrUpdateBot(string callsign, PutBotRequestDto dto, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveBot(string callsign, CancellationToken? cancellationToken = null)
    {
        return Task.CompletedTask;
    }
}
