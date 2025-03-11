// <copyright file="GetChangeProfileReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using WatsonWebsocket;

namespace Vatsim.Vatis.Events.WebSocket;

/// <summary>
/// Represents an event that is raised by a websocket client to change the profile.
/// </summary>
/// <param name="Session">The websocket client making the request.</param>
/// <param name="ProfileId">The unique profile identifier.</param>
public record GetChangeProfileReceived(ClientMetadata Session, string? ProfileId) : IEvent;
