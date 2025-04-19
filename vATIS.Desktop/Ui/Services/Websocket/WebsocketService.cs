// <copyright file="WebsocketService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.WebSocket;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Services.Websocket.Messages;
using WatsonWebsocket;

namespace Vatsim.Vatis.Ui.Services.Websocket;

/// <summary>
/// Provides a websocket interface to vATIS.
/// </summary>
public class WebsocketService : IWebsocketService
{
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;

    // The websocket server.
    private readonly WatsonWsServer _server;

    // A list of connected clients so messages can be broadcast to all connected clients when requested.
    private readonly ConcurrentDictionary<Guid, ClientMetadata> _sessions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebsocketService"/> class.
    /// </summary>
    /// <param name="profileRepository">The profile repository service.</param>
    /// <param name="sessionManager">The session manager service.</param>
    public WebsocketService(IProfileRepository profileRepository, ISessionManager sessionManager)
    {
        _profileRepository = profileRepository;
        _sessionManager = sessionManager;

        // The loopback address is used to avoid Windows prompting for firewall permissions
        // when vATIS runs.
        _server = new WatsonWsServer(hostname: IPAddress.Loopback.ToString(), port: 49082);
        _server.Logger = Log.Information;
        _server.ClientConnected += OnClientConnected;
        _server.ClientDisconnected += OnClientDisconnected;
        _server.MessageReceived += OnMessageReceived;
    }

    /// <inheritdoc />
    public event EventHandler<GetAtisReceived> GetAtisReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<GetStationListReceived> GetStationsReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<AcknowledgeAtisUpdateReceived> AcknowledgeAtisUpdateReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<GetConfigureAtisReceived> ConfigureAtisReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<GetConnectAtisReceived> ConnectAtisReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<GetDisconnectAtisReceived> DisconnectAtisReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler<GetChangeProfileReceived> ChangeProfileReceived = (_, _) => { };

    /// <inheritdoc />
    public event EventHandler ExitApplicationReceived = (_, _) => { };

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
    /// Sends an ATIS message to a specific session or to all connected clients if the session is null.
    /// </summary>
    /// <param name="session">The session to send the message to.</param>
    /// <param name="value">The value to send.</param>
    /// <returns>A task.</returns>
    public async Task SendAtisMessageAsync(ClientMetadata? session, AtisMessage.AtisMessageValue value)
    {
        var message = new AtisMessage { Value = value, };

        if (session is not null)
        {
            await _server.SendAsync(session.Guid,
                JsonSerializer.Serialize(message, SourceGenerationContext.NewDefault.AtisMessage));
        }
        else
        {
            await SendAsync(JsonSerializer.Serialize(message, SourceGenerationContext.NewDefault.AtisMessage));
        }
    }

    /// <inheritdoc />
    public async Task SendAtisStationsAsync(ClientMetadata? session, AtisStationMessage value)
    {
        if (session is not null)
        {
            await _server.SendAsync(session.Guid,
                JsonSerializer.Serialize(value, SourceGenerationContext.NewDefault.AtisStationMessage));
        }
        else
        {
            await SendAsync(JsonSerializer.Serialize(value, SourceGenerationContext.NewDefault.AtisStationMessage));
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
            await HandleRequest(e.Client, e.Data);
        }
        catch (TaskCanceledException)
        {
            // Ignore
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WebSocket Exception");
            var error = new ErrorMessage { Value = new ErrorMessage.ErrorValue { Message = ex.Message, }, };
            await _server.SendAsync(e.Client.Guid,
                JsonSerializer.Serialize(error, SourceGenerationContext.NewDefault.ErrorMessage));
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
    /// Handles a request from a client. It looks at the type property to determine the message type,
    /// then fires the appropriate event with the session and station as parameters.
    /// </summary>
    /// <param name="session">The client that sent the message.</param>
    /// <param name="message">The message.</param>
    /// <exception cref="ArgumentException">Thrown if the type is missing or invalid.</exception>
    private async Task HandleRequest(ClientMetadata session, ArraySegment<byte> message)
    {
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProperty))
        {
            throw new ArgumentException("Invalid request: no message type specified");
        }

        var messageType = typeProperty.GetString() ??
                          throw new ArgumentException("Invalid request: no message type specified");

        var commandMessageTypes = new HashSet<string>
        {
            "acknowledgeAtisUpdate",
            "getAtis",
            "getProfiles",
            "getStations",
            "getActiveProfile",
            "getContractions",
            "quit"
        };

        if (commandMessageTypes.Contains(messageType))
        {
            var request =
                JsonSerializer.Deserialize(root.GetRawText(), SourceGenerationContext.NewDefault.CommandMessage) ??
                throw new ArgumentException("Invalid request: no message value specified");

            switch (messageType)
            {
                case "acknowledgeAtisUpdate":
                    AcknowledgeAtisUpdateReceived(this,
                        new AcknowledgeAtisUpdateReceived(session, request.Value?.StationId, request.Value?.Station,
                            request.Value?.AtisType));
                    break;
                case "getAtis":
                    GetAtisReceived(this,
                        new GetAtisReceived(session, request.Value?.StationId, request.Value?.Station,
                            request.Value?.AtisType));
                    break;
                case "getProfiles":
                    await HandleGetInstalledProfiles(session);
                    break;
                case "getStations":
                    GetStationsReceived(this, new GetStationListReceived(session));
                    break;
                case "getActiveProfile":
                    await HandleGetActiveProfile(session);
                    break;
                case "getContractions":
                    await HandleGetContractions(session, request.Value?.StationId, request.Value?.Station,
                        request.Value?.AtisType);
                    break;
                case "quit":
                    ExitApplicationReceived(this, EventArgs.Empty);
                    break;
                default:
                    throw new ArgumentException($"Invalid request: unknown message type {messageType}");
            }
        }
        else
        {
            switch (messageType)
            {
                case "loadProfile":
                {
                    var request = JsonSerializer.Deserialize(root.GetRawText(),
                                      SourceGenerationContext.NewDefault.LoadProfileMessage) ??
                                  throw new ArgumentException("Invalid request: no message value specified");
                    ChangeProfileReceived(this, new GetChangeProfileReceived(session, request.Payload?.ProfileId));
                    break;
                }

                case "configureAtis":
                {
                    var request = JsonSerializer.Deserialize(root.GetRawText(),
                                      SourceGenerationContext.NewDefault.ConfigureAtisMessage) ??
                                  throw new ArgumentException("Invalid request: no message value specified");
                    ConfigureAtisReceived(this, new GetConfigureAtisReceived(session, request.Payload));
                    break;
                }

                case "connectAtis":
                {
                    var request = JsonSerializer.Deserialize(root.GetRawText(),
                                      SourceGenerationContext.NewDefault.ConnectAtisMessage) ??
                                  throw new ArgumentException("Invalid request: no message value specified");
                    ConnectAtisReceived(this, new GetConnectAtisReceived(session, request.Payload));
                    break;
                }

                case "disconnectAtis":
                {
                    var request = JsonSerializer.Deserialize(root.GetRawText(),
                                      SourceGenerationContext.NewDefault.DisconnectAtisMessage) ??
                                  throw new ArgumentException("Invalid request: no message value specified");
                    DisconnectAtisReceived(this, new GetDisconnectAtisReceived(session, request.Payload));
                    break;
                }

                default:
                    throw new ArgumentException($"Invalid request: unknown message type {messageType}");
            }
        }
    }

    private async Task HandleGetContractions(ClientMetadata? session, string? stationId, string? station,
        AtisType? atisType)
    {
        var currentProfile = _sessionManager.CurrentProfile ?? throw new Exception("No active profile.");
        if (!string.IsNullOrEmpty(stationId) && !string.IsNullOrEmpty(station))
            throw new Exception("Cannot provide both Id and Station.");

        var stationsList = currentProfile.Stations;
        if (stationsList == null)
            return;

        var resultMessage = new ContractionsResponseMessage { Stations = [] };

        // Case 1: Request for all stations
        if (string.IsNullOrEmpty(stationId) && string.IsNullOrEmpty(station))
        {
            foreach (var s in stationsList)
            {
                var contractionsDict = s.Contractions
                    .Where(c => !string.IsNullOrWhiteSpace(c.VariableName))
                    .ToDictionary(
                        c => c.VariableName!,
                        c => new ContractionsResponseMessage.StationContractions.ContractionDetail
                        {
                            Text = c.Text, Voice = c.Voice
                        });

                resultMessage.Stations.Add(new ContractionsResponseMessage.StationContractions
                {
                    Id = s.Id, Name = s.Name, AtisType = s.AtisType, Contractions = contractionsDict
                });
            }
        }

        // Case 2: Request for a specific station
        else
        {
            var targetStation = stationsList.FirstOrDefault(x =>
                x.Id == stationId ||
                (x.Identifier.Equals(station, StringComparison.InvariantCultureIgnoreCase) && x.AtisType == atisType));

            if (targetStation == null)
                return;

            var contractionsDict = targetStation.Contractions
                .Where(c => !string.IsNullOrWhiteSpace(c.VariableName))
                .ToDictionary(
                    c => c.VariableName!,
                    c => new ContractionsResponseMessage.StationContractions.ContractionDetail
                    {
                        Text = c.Text, Voice = c.Voice
                    });

            resultMessage.Stations.Add(new ContractionsResponseMessage.StationContractions
            {
                Id = targetStation.Id,
                Name = targetStation.Name,
                AtisType = targetStation.AtisType,
                Contractions = contractionsDict
            });
        }

        var serialized = JsonSerializer.Serialize(resultMessage,
            SourceGenerationContext.NewDefault.ContractionsResponseMessage);

        if (session is not null)
            await _server.SendAsync(session.Guid, serialized);
        else
            await SendAsync(serialized);
    }

    private async Task HandleGetActiveProfile(ClientMetadata? session)
    {
        var currentProfile = _sessionManager.CurrentProfile ?? throw new Exception("No active profile.");
        var message = new ActiveProfileMessage { Id = currentProfile.Id, Name = currentProfile.Name };

        if (session is not null)
        {
            await _server.SendAsync(session.Guid,
                JsonSerializer.Serialize(message, SourceGenerationContext.NewDefault.ActiveProfileMessage));
        }
        else
        {
            await SendAsync(JsonSerializer.Serialize(message,
                SourceGenerationContext.NewDefault.ActiveProfileMessage));
        }
    }

    private async Task HandleGetInstalledProfiles(ClientMetadata? session)
    {
        var profiles = await _profileRepository.LoadAll();
        var list = profiles.Select(profile =>
                new InstalledProfilesMessage.ProfileEntity { Id = profile.Id, Name = profile.Name })
            .OrderBy(p => p.Name)
            .ToList();

        var message = new InstalledProfilesMessage { Profiles = [..list] };

        if (session is not null)
        {
            await _server.SendAsync(session.Guid,
                JsonSerializer.Serialize(message, SourceGenerationContext.NewDefault.InstalledProfilesMessage));
        }
        else
        {
            await SendAsync(JsonSerializer.Serialize(message,
                SourceGenerationContext.NewDefault.InstalledProfilesMessage));
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
                    Log.Error(ex, "Error closing session {SessionGuid}", session.Guid);
                }

                return Task.CompletedTask;
            }));
        }

        await Task.WhenAll(tasks);
        _sessions.Clear();
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
                    Log.Error(ex, "Error sending message to session {SessionGuid}", session.Guid);
                }
            }));
        }

        await Task.WhenAll(tasks);
    }
}
