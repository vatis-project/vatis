// <copyright file="WebsocketService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;
using WatsonWebsocket;

namespace Vatsim.Vatis.Ui.Services;

/// <summary>
/// Provides a websocket interface to vATIS.
/// </summary>
public class WebsocketService : IWebsocketService
{
    // The websocket server.
    private readonly WatsonWsServer _server;

    // A list of connected clients so messages can be broadcast to all connected clients when requested.
    private readonly ConcurrentDictionary<Guid, ClientMetadata> _sessions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebsocketService"/> class.
    /// </summary>
    public WebsocketService()
    {
        // The loopback address is used to avoid Windows prompting for firewall permissions
        // when vATIS runs.
        _server = new WatsonWsServer(hostname: IPAddress.Loopback.ToString(), port: 49082);
        _server.Logger = Log.Information;
        _server.ClientConnected += OnClientConnected;
        _server.ClientDisconnected += OnClientDisconnected;
        _server.MessageReceived += OnMessageReceived;
    }

    /// <summary>
    /// Event that is raised when a client requests ATIS information. The requesting session and the station requested, if specified, are passed as parameters.
    /// </summary>
    ///
    public event EventHandler<GetAtisReceived> GetAtisReceived = (_, _) => { };

    /// <summary>
    /// Event that is raised when a client acknowledges an ATIS update. The requesting session and the station acknowledged, if specified, is passed as a parameter.
    /// </summary>
    public event EventHandler<AcknowledgeAtisUpdateReceived> AcknowledgeAtisUpdateReceived = (_, _) => { };

    /// <summary>
    /// Starts the WebSocket server.
    /// </summary>
    /// <returns>A task.</returns>
    public async Task StartAsync()
    {
        try
        {
            await _server.StartAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to start WebSocket server");
        }
    }

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    /// <returns>A task.</returns>
    public async Task StopAsync()
    {
        try
        {
            await CloseAllClientsAsync();
            _server.Stop();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to stop WebSocket server");
        }
    }

    /// <summary>
    /// Sends an ATIS message to a specific session, or to all connected clients if session is null.
    /// </summary>
    /// <param name="session">The session to send the message to.</param>
    /// <param name="value">The value to send.</param>
    /// <returns>A task.</returns>
    public async Task SendAtisMessage(ClientMetadata? session, AtisMessage.AtisMessageValue value)
    {
        var message = new AtisMessage
        {
            Value = value,
        };

        if (session is not null)
        {
            await _server.SendAsync(session.Guid, JsonSerializer.Serialize(message, SourceGenerationContext.NewDefault.AtisMessage));
        }
        else
        {
            await SendAsync(JsonSerializer.Serialize(message, SourceGenerationContext.NewDefault.AtisMessage));
        }
    }

    /// <summary>
    /// Handles messages received via the websocket and fires the appropriate event handler.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The message data.</param>
    private async void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            HandleRequest(e.Client, e.Data);
        }
        catch (Exception ex)
        {
            var error = new ErrorMessage
            {
                Value = new ErrorMessage.ErrorValue
                {
                    Message = ex.Message,
                },
            };
            await _server.SendAsync(e.Client.Guid, JsonSerializer.Serialize(error, SourceGenerationContext.NewDefault.ErrorMessage));
        }
    }

    /// <summary>
    /// Handles clients disconnecting from the service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The data about the client that disconnected.</param>
    private void OnClientDisconnected(object? sender, DisconnectionEventArgs e)
    {
        _sessions.TryRemove(e.Client.Guid, out _);
    }

    /// <summary>
    /// Handles clients connecting to the service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The data about the client that connected.</param>
    private void OnClientConnected(object? sender, ConnectionEventArgs e)
    {
        _sessions.TryAdd(e.Client.Guid, e.Client);
    }

    /// <summary>
    /// Handles a request from a client. Looks at the type property to determine the message type
    /// then fires the appropriate event with the session and station as parameters.
    /// </summary>
    /// <param name="session">The client that sent the message.</param>
    /// <param name="message">The message.</param>
    /// <exception cref="ArgumentException">Thrown if the type is missing or invalid.</exception>
    private void HandleRequest(ClientMetadata session, ArraySegment<byte> message)
    {
        var request = JsonSerializer.Deserialize(message, SourceGenerationContext.NewDefault.CommandMessage);

        if (request == null || string.IsNullOrWhiteSpace(request.MessageType))
        {
            throw new ArgumentException("Invalid request: no message type specified");
        }

        switch (request.MessageType)
        {
            case "getAtis":
                GetAtisReceived(this, new GetAtisReceived(session, request.Value?.Station, request.Value?.AtisType));
                break;
            case "acknowledgeAtisUpdate":
                AcknowledgeAtisUpdateReceived(this, new AcknowledgeAtisUpdateReceived(session, request.Value?.Station, request.Value?.AtisType));
                break;
            default:
                throw new ArgumentException($"Invalid request: unknown message type {request.MessageType}");
        }
    }

    /// <summary>
    /// Closes all connected sessions.
    /// </summary>
    /// <returns>A task.</returns>
    private async Task CloseAllClientsAsync()
    {
        var tasks = new List<Task>();

        foreach (var session in _sessions.Values)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _server.DisconnectClient(session.Guid);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error closing session {session.Guid}");
                }

                return Task.CompletedTask;
            }));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Sends a message to all connected clients.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <returns>A task.</returns>
    private async Task SendAsync(string message)
    {
        var tasks = new List<Task>();

        foreach (var session in _sessions.Values)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _server.SendAsync(session.Guid, message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error sending message to session {session.Guid}");
                }
            }));
        }

        await Task.WhenAll(tasks);
    }
}
