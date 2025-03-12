// <copyright file="IWebsocketService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading.Tasks;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.WebSocket;
using Vatsim.Vatis.Ui.Services.Websocket.Messages;
using WatsonWebsocket;

namespace Vatsim.Vatis.Ui.Services.Websocket;

/// <summary>
/// Represents a WebsocketService.
/// </summary>
public interface IWebsocketService
{
    /// <summary>
    /// Event that is raised when a client acknowledges an ATIS update. The requesting session and the station acknowledged, if specified, is passed as a parameter.
    /// </summary>
    public event EventHandler<AcknowledgeAtisUpdateReceived> AcknowledgeAtisUpdateReceived;

    /// <summary>
    /// Event that is raised when a client requests ATIS information. The requesting session and the station requested, if specified, are passed as parameters.
    /// </summary>
    public event EventHandler<GetAtisReceived> GetAtisReceived;

    /// <summary>
    /// Event that is raised when a client requests a list of a ATIS stations.
    /// </summary>
    public event EventHandler<GetStationListReceived> GetStationsReceived;

    /// <summary>
    /// Event that is raised by a websocket client to configure an ATIS station.
    /// </summary>
    public event EventHandler<GetConfigureAtisReceived> ConfigureAtisReceived;

    /// <summary>
    /// Event that is raised by a websocket client to connect an ATIS.
    /// </summary>
    public event EventHandler<GetConnectAtisReceived> ConnectAtisReceived;

    /// <summary>
    /// Event that is raised by a websocket client to disconnect an ATIS.
    /// </summary>
    public event EventHandler<GetDisconnectAtisReceived> DisconnectAtisReceived;

    /// <summary>
    /// Event that is raised by a websocket client to change the active profile.
    /// </summary>
    public event EventHandler<GetChangeProfileReceived> LoadProfileRequested;

    /// <summary>
    /// Event that is raised by a websocket client to exit the application.
    /// </summary>
    public event EventHandler ApplicationExitRequested;

    /// <summary>
    /// Sends an ATIS message to a specific session, or to all connected clients if session is null.
    /// </summary>
    /// <param name="session">The session to send the message to.</param>
    /// <param name="value">The value to send.</param>
    /// <returns>A task.</returns>
    Task SendAtisMessage(ClientMetadata? session, AtisMessage.AtisMessageValue value);

    /// <summary>
    /// Sends a message to the specific session with the list of ATIS preset names,
    /// or to all connected clients if session is null.
    /// </summary>
    /// <param name="session">The session to send the message to.</param>
    /// <param name="value">The value to send.</param>
    /// <returns>A task.</returns>
    Task SendAtisPresets(ClientMetadata? session, AtisPresetMessage value);

    /// <summary>
    /// Sends a message with a list of ATIS stations to the specific client session,
    /// or to all connected clients if the session is null.
    /// </summary>
    /// <param name="session">The session to send the message to.</param>
    /// <param name="value">The value to send.</param>
    /// <returns>A task.</returns>
    Task SendAtisStations(ClientMetadata? session, AtisStationMessage value);

    /// <summary>
    /// Starts the WebSocket server.
    /// </summary>
    /// <returns>A task.</returns>
    Task StartAsync();

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    /// <returns>A task.</returns>
    Task StopAsync();
}
