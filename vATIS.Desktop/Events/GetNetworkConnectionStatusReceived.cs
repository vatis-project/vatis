// <copyright file="GetNetworkConnectionStatusReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using WatsonWebsocket;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the GetNetworkConnectionStatusReceived event.
/// </summary>
/// <param name="Session">The client that requested the network status.</param>
/// <param name="Station">The station to get the network status for. If null all stations are returned.</param>
public record GetNetworkConnectionStatusReceived(ClientMetadata Session, string? Station) : IEvent;
