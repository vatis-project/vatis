// <copyright file="GetConnectAtisReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Ui.Services.Websocket.Messages;
using WatsonWebsocket;

namespace Vatsim.Vatis.Events.WebSocket;

/// <summary>
/// Represents an event raised by a websocket client to connect an ATIS.
/// </summary>
/// <param name="Session">The websocket client making the request.</param>
/// <param name="Payload">The message payload.</param>
public record GetConnectAtisReceived(ClientMetadata Session, ConnectAtisMessage.ConnectMessagePayload? Payload) : IEvent;
