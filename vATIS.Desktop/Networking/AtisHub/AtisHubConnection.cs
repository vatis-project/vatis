// <copyright file="AtisHubConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using Vatsim.Network;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Networking.AtisHub;

/// <inheritdoc />
public class AtisHubConnection : IAtisHubConnection
{
    private readonly IAppConfigurationProvider appConfigurationProvider;
    private readonly IClientAuth clientAuth;
    private ConnectionState connectionState;
    private HubConnection? hubConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisHubConnection"/> class.
    /// </summary>
    /// <param name="appConfigurationProvider">The application configuration provider used for accessing configuration settings.</param>
    /// <param name="clientAuth">The client authentication mechanism for validating requests to the ATIS hub.</param>
    public AtisHubConnection(IAppConfigurationProvider appConfigurationProvider, IClientAuth clientAuth)
    {
        this.appConfigurationProvider = appConfigurationProvider;
        this.clientAuth = clientAuth;
    }

    /// <inheritdoc/>
    public async Task Connect()
    {
        try
        {
            if (this.hubConnection is { State: HubConnectionState.Connected })
            {
                return;
            }

            var serverUrl = this.appConfigurationProvider.AtisHubUrl;

            this.hubConnection = new HubConnectionBuilder()
                .WithUrl(
                    serverUrl,
                    options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(this.clientAuth.GenerateHubToken());
                    })
                .AddJsonProtocol(
                    options =>
                    {
                        options.PayloadSerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.NewDefault);
                    })
                .WithAutomaticReconnect()
                .Build();

            this.hubConnection.Closed += this.OnHubConnectionClosed;
            this.hubConnection.On<List<AtisHubDto>>(
                "AtisReceived",
                dtoList =>
                {
                    foreach (var dto in dtoList)
                    {
                        MessageBus.Current.SendMessage(new AtisHubAtisReceived(dto));
                    }
                });
            this.hubConnection.On<AtisHubDto>(
                "RemoveAtisReceived",
                dto => { MessageBus.Current.SendMessage(new AtisHubExpiredAtisReceived(dto)); });

            this.SetConnectionState(ConnectionState.Connecting);
            Log.Information($"Connecting to AtisHub server: {serverUrl}");
            await this.hubConnection.StartAsync();
            Log.Information("Connected to AtisHub with ID: " + this.hubConnection.ConnectionId);
            this.SetConnectionState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            this.SetConnectionState(ConnectionState.Disconnected);
            Log.Error(ex.Message, "Failed to connect to AtisHub.");
        }
    }

    /// <inheritdoc/>
    public async Task Disconnect()
    {
        if (this.hubConnection == null)
        {
            return;
        }

        try
        {
            await this.hubConnection.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message, "Failed to disconnect from AtisHub.");
        }
    }

    /// <inheritdoc/>
    public async Task PublishAtis(AtisHubDto dto)
    {
        if (this.hubConnection is not { State: HubConnectionState.Connected })
        {
            return;
        }

        await this.hubConnection.InvokeAsync("PublishAtis", dto);
    }

    /// <inheritdoc/>
    public async Task SubscribeToAtis(SubscribeDto dto)
    {
        if (this.hubConnection is not { State: HubConnectionState.Connected })
        {
            return;
        }

        await this.hubConnection.InvokeAsync("SubscribeToAtis", dto);
    }

    private Task OnHubConnectionClosed(Exception? exception)
    {
        if (exception != null)
        {
            Log.Error(exception, "AtisHub connection closed unexpectedly.");
        }

        Log.Information("Disconnected from AtisHub.");
        this.SetConnectionState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    private void SetConnectionState(ConnectionState state)
    {
        this.connectionState = state;
        MessageBus.Current.SendMessage(new ConnectionStateChanged(this.connectionState));
        switch (this.connectionState)
        {
            case ConnectionState.Connected:
                MessageBus.Current.SendMessage(new HubConnected());
                break;
            case ConnectionState.Disconnected:
                MessageBus.Current.SendMessage(new HubDisconnected());
                break;
        }
    }
}
