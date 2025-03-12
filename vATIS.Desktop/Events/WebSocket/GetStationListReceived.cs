// <copyright file="GetStationListReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using WatsonWebsocket;

namespace Vatsim.Vatis.Events.WebSocket;

/// <summary>
/// Represents an event that is raised to request a list of all stations in the loaded profile.
/// </summary>
/// <param name="Session">The websocket client that made the request.</param>
public record GetStationListReceived(ClientMetadata Session) : IEvent;
