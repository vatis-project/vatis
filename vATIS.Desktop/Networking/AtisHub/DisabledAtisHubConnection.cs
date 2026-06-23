// <copyright file="DisabledAtisHubConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Vatsim.Vatis.Networking.AtisHub.Dto;

namespace Vatsim.Vatis.Networking.AtisHub;

/// <summary>
/// Provides a no-op ATIS hub connection for runtime modes that disable hub traffic.
/// </summary>
public class DisabledAtisHubConnection : IAtisHubConnection
{
    /// <inheritdoc />
    public Task Connect() => Task.CompletedTask;

    /// <inheritdoc />
    public Task Disconnect() => Task.CompletedTask;

    /// <inheritdoc />
    public Task PublishAtis(AtisHubDto dto) => Task.CompletedTask;

    /// <inheritdoc />
    public Task SubscribeToAtis(SubscribeDto dto) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<char?> GetDigitalAtisLetter(DigitalAtisRequestDto dto) => Task.FromResult<char?>(null);

    /// <inheritdoc />
    public Task DisconnectAtis(AtisHubDto dto) => Task.CompletedTask;
}
