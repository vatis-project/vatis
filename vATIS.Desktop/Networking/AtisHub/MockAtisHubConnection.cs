// <copyright file="MockAtisHubConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Weather.Decoder;

namespace Vatsim.Vatis.Networking.AtisHub;

/// <inheritdoc />
public class MockAtisHubConnection : IAtisHubConnection
{
    private HubConnection? hubConnection;
    private ConnectionState hubConnectionState;

    /// <inheritdoc/>
    public async Task Connect()
    {
        try
        {
            if (this.hubConnection is { State: HubConnectionState.Connected })
            {
                return;
            }

            this.hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://{IPAddress.Loopback.ToString()}:5500/hub")
                .WithAutomaticReconnect()
                .AddJsonProtocol(
                    options =>
                    {
                        options.PayloadSerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.NewDefault);
                    })
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
            this.hubConnection.On<string>(
                "MetarReceived",
                message =>
                {
                    try
                    {
                        var decoder = new MetarDecoder();
                        var metar = decoder.ParseStrict(message);
                        MessageBus.Current.SendMessage(new MetarReceived(metar));
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error parsing dev METAR");
                    }
                });

            this.SetConnectionState(ConnectionState.Connecting);
            Log.Information("Connecting to Dev AtisHub.");
            await this.hubConnection.StartAsync();
            Log.Information("Connected to Dev AtisHub with ID: " + this.hubConnection.ConnectionId);
            this.SetConnectionState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            this.SetConnectionState(ConnectionState.Disconnected);
            Log.Error(ex.Message, "Failed to connect to Dev AtisHub.");
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
            Log.Error(ex.Message, "Failed to disconnect from Dev AtisHub.");
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

    private void SetConnectionState(ConnectionState connectionState)
    {
        this.hubConnectionState = connectionState;
        MessageBus.Current.SendMessage(new ConnectionStateChanged(this.hubConnectionState));
        switch (this.hubConnectionState)
        {
            case ConnectionState.Connected:
                MessageBus.Current.SendMessage(new HubConnected());
                break;
            case ConnectionState.Disconnected:
                MessageBus.Current.SendMessage(new HubDisconnected());
                break;
        }
    }

    private Task OnHubConnectionClosed(Exception? exception)
    {
        if (exception != null)
        {
            Log.Error(exception, "Dev AtisHub connection closed unexpectedly.");
        }

        this.SetConnectionState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }
}
