// <copyright file="AtisHubConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Vatsim.Network;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Networking.AtisHub.Dto;

namespace Vatsim.Vatis.Networking.AtisHub;

/// <summary>
/// Represents a connection to the AtisHub server. Implements <see cref="IAtisHubConnection"/>.
/// </summary>
public class AtisHubConnection : IAtisHubConnection
{
    /// <summary>
    /// Text the hub puts at the start of the error it returns when the user already has the maximum number of ATISes
    /// published.
    /// </summary>
    public const string AtisLimitExceededMarker = "AtisLimitExceeded";

    private readonly IAuthTokenManager _authTokenManager;
    private readonly IAppConfigurationProvider _appConfigurationProvider;
    private HubConnection? _hubConnection;
    private ConnectionState _connectionState;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisHubConnection"/> class.
    /// </summary>
    /// <param name="appConfigurationProvider">
    /// The provider for application configurations.
    /// </param>
    /// <param name="authTokenManager">
    /// Provides the VATSIM-issued token the hub authenticates the user with.
    /// </param>
    public AtisHubConnection(
        IAppConfigurationProvider appConfigurationProvider,
        IAuthTokenManager authTokenManager)
    {
        _appConfigurationProvider = appConfigurationProvider;
        _authTokenManager = authTokenManager;
    }

    /// <inheritdoc />
    public async Task Connect()
    {
        try
        {
            if (_hubConnection is { State: HubConnectionState.Connected })
                return;

            // Fail fast, before ever touching SignalR: GetAuthToken throwing from inside
            // AccessTokenProvider does NOT abort StartAsync -- the client swallows that and
            // proceeds without a token, so the server accepts the transport, then closes it a
            // moment later once it checks auth itself. Checking here instead means a missing/bad
            // token never gets reported as "Connected" at all.
            await _authTokenManager.GetAuthToken();

            var serverUrl = _appConfigurationProvider.AtisHubUrl;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl, options =>
                {
                    options.AccessTokenProvider = GetAccessToken;
                })
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.NewDefault);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.Closed += OnHubConnectionClosed;
            _hubConnection.Reconnecting += _ =>
            {
                SetConnectionState(ConnectionState.Connecting);
                return Task.CompletedTask;
            };
            _hubConnection.Reconnected += connectionId =>
            {
                // The hub assigns a new connection ID on reconnect, so subscriptions and published ATISes
                // are lost. Raising HubConnected makes stations re-subscribe and republish.
                Log.Information("Reconnected to AtisHub with ID: " + connectionId);
                SetConnectionState(ConnectionState.Connected);
                return Task.CompletedTask;
            };
            _hubConnection.On<List<AtisHubDto>>("AtisReceived", (dtoList) =>
            {
                foreach (var dto in dtoList)
                {
                    EventBus.Instance.Publish(new AtisHubAtisReceived(dto));
                }
            });
            _hubConnection.On<AtisHubDto>("RemoveAtisReceived", (dto) =>
            {
                EventBus.Instance.Publish(new AtisHubExpiredAtisReceived(dto));
            });

            SetConnectionState(ConnectionState.Connecting);
            Log.Information($"Connecting to AtisHub server: {serverUrl}");
            await _hubConnection.StartAsync();
            Log.Information("Connected to AtisHub with ID: " + _hubConnection.ConnectionId);
            SetConnectionState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            SetConnectionState(ConnectionState.Disconnected);
            Log.Error(ex, "Failed to connect to AtisHub.");
        }
    }

    /// <inheritdoc />
    public async Task Disconnect()
    {
        if (_hubConnection == null)
            return;

        try
        {
            await _hubConnection.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to disconnect from AtisHub.");
        }
    }

    /// <inheritdoc />
    public async Task ReserveAtis(AtisHubDto dto)
    {
        // The first connection attempt at startup may have failed, and SignalR only retries after it has connected once.
        if (_hubConnection is null or { State: HubConnectionState.Disconnected })
        {
            await Connect();
        }

        if (_hubConnection is not { State: HubConnectionState.Connected })
            throw new InvalidOperationException("The ATIS hub is not connected.");

        await _hubConnection.InvokeAsync("ReserveAtis", dto);
        Log.Information("ReserveAtis: {DtoStationId}", dto.StationId);
    }

    /// <inheritdoc />
    public async Task PublishAtis(AtisHubDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return;

        await _hubConnection.InvokeAsync("PublishAtis", dto);
        Log.Information("PublishAtis: {DtoStationId}", dto.StationId);
    }

    /// <inheritdoc />
    public async Task SubscribeToAtis(SubscribeDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return;

        await _hubConnection.InvokeAsync("SubscribeToAtis", dto);
        Log.Information("SubscribeToAtis: {DtoStationId}", dto.StationId);
    }

    /// <inheritdoc />
    public async Task<char?> GetDigitalAtisLetter(DigitalAtisRequestDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return null;

        Log.Information("GetDigitalAtisLetter: {DtoStationId}", dto.Id);
        return await _hubConnection.InvokeAsync<char>("GetDigitalAtisLetter", dto);
    }

    /// <inheritdoc />
    public async Task DisconnectAtis(AtisHubDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return;

        await _hubConnection.InvokeAsync("DisconnectAtis", dto);
        Log.Information("DisconnectAtis: {DtoStationId}", dto.StationId);
    }

    /// <summary>
    /// Gets the token to authenticate with the hub. SignalR calls this on every connect and reconnect.
    /// </summary>
    /// <remarks>
    /// The hub only accepts the user's VATSIM FSD token, which identifies who is connecting and expires within
    /// minutes. If no token can be obtained, the connection attempt fails.
    /// </remarks>
    private async Task<string?> GetAccessToken()
    {
        try
        {
            var token = await _authTokenManager.GetAuthToken();
            if (string.IsNullOrEmpty(token))
                throw new AuthTokenException("No VATSIM token available for AtisHub.");

            return token;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not get a VATSIM token for AtisHub.");
            throw;
        }
    }

    private Task OnHubConnectionClosed(Exception? exception)
    {
        if (exception != null)
        {
            Log.Error(exception, "AtisHub connection closed unexpectedly.");
        }

        Log.Information("Disconnected from AtisHub.");
        SetConnectionState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    private void SetConnectionState(ConnectionState connectionState)
    {
        _connectionState = connectionState;
        EventBus.Instance.Publish(new ConnectionStateChanged(_connectionState));
        switch (_connectionState)
        {
            case ConnectionState.Connected:
                EventBus.Instance.Publish(new HubConnected());
                break;
            case ConnectionState.Disconnected:
                EventBus.Instance.Publish(new HubDisconnected());
                break;
        }
    }
}
