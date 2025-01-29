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
    private readonly IClientAuth _clientAuth;
    private readonly IAppConfigurationProvider _appConfigurationProvider;
    private HubConnection? _hubConnection;
    private ConnectionState _connectionState;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisHubConnection"/> class.
    /// </summary>
    /// <param name="appConfigurationProvider">
    /// The provider for application configurations.
    /// </param>
    /// <param name="clientAuth">
    /// The client authentication instance.
    /// </param>
    public AtisHubConnection(IAppConfigurationProvider appConfigurationProvider, IClientAuth clientAuth)
    {
        _appConfigurationProvider = appConfigurationProvider;
        _clientAuth = clientAuth;
    }

    /// <inheritdoc />
    public async Task Connect()
    {
        try
        {
            if (_hubConnection is { State: HubConnectionState.Connected })
                return;

            var serverUrl = _appConfigurationProvider.AtisHubUrl;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_clientAuth.GenerateHubToken());
                })
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.NewDefault);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.Closed += OnHubConnectionClosed;
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
            Log.Error(ex.Message, "Failed to connect to AtisHub.");
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
            Log.Error(ex.Message, "Failed to disconnect from AtisHub.");
        }
    }

    /// <inheritdoc />
    public async Task PublishAtis(AtisHubDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return;

        await _hubConnection.InvokeAsync("PublishAtis", dto);
    }

    /// <inheritdoc />
    public async Task SubscribeToAtis(SubscribeDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return;

        await _hubConnection.InvokeAsync("SubscribeToAtis", dto);
    }

    /// <inheritdoc />
    public async Task<char?> GetDigitalAtisLetter(DigitalAtisRequestDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return null;

        return await _hubConnection.InvokeAsync<char>("GetDigitalAtisLetter", dto);
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
