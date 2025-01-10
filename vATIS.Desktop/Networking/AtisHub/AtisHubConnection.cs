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

public class AtisHubConnection : IAtisHubConnection
{
    private readonly IAppConfigurationProvider _appConfigurationProvider;
    private readonly IClientAuth _clientAuth;
    private ConnectionState _connectionState;
    private HubConnection? _hubConnection;

    public AtisHubConnection(IAppConfigurationProvider appConfigurationProvider, IClientAuth clientAuth)
    {
        this._appConfigurationProvider = appConfigurationProvider;
        this._clientAuth = clientAuth;
    }

    public async Task Connect()
    {
        try
        {
            if (this._hubConnection is { State: HubConnectionState.Connected })
            {
                return;
            }

            var serverUrl = this._appConfigurationProvider.AtisHubUrl;

            this._hubConnection = new HubConnectionBuilder()
                .WithUrl(
                    serverUrl,
                    options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(this._clientAuth.GenerateHubToken());
                    })
                .AddJsonProtocol(
                    options =>
                    {
                        options.PayloadSerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.NewDefault);
                    })
                .WithAutomaticReconnect()
                .Build();

            this._hubConnection.Closed += this.OnHubConnectionClosed;
            this._hubConnection.On<List<AtisHubDto>>(
                "AtisReceived",
                dtoList =>
                {
                    foreach (var dto in dtoList)
                    {
                        MessageBus.Current.SendMessage(new AtisHubAtisReceived(dto));
                    }
                });
            this._hubConnection.On<AtisHubDto>(
                "RemoveAtisReceived",
                dto => { MessageBus.Current.SendMessage(new AtisHubExpiredAtisReceived(dto)); });

            this.SetConnectionState(ConnectionState.Connecting);
            Log.Information($"Connecting to AtisHub server: {serverUrl}");
            await this._hubConnection.StartAsync();
            Log.Information("Connected to AtisHub with ID: " + this._hubConnection.ConnectionId);
            this.SetConnectionState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            this.SetConnectionState(ConnectionState.Disconnected);
            Log.Error(ex.Message, "Failed to connect to AtisHub.");
        }
    }

    public async Task Disconnect()
    {
        if (this._hubConnection == null)
        {
            return;
        }

        try
        {
            await this._hubConnection.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message, "Failed to disconnect from AtisHub.");
        }
    }

    public async Task PublishAtis(AtisHubDto dto)
    {
        if (this._hubConnection is not { State: HubConnectionState.Connected })
        {
            return;
        }

        await this._hubConnection.InvokeAsync("PublishAtis", dto);
    }

    public async Task SubscribeToAtis(SubscribeDto dto)
    {
        if (this._hubConnection is not { State: HubConnectionState.Connected })
        {
            return;
        }

        await this._hubConnection.InvokeAsync("SubscribeToAtis", dto);
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

    private void SetConnectionState(ConnectionState connectionState)
    {
        this._connectionState = connectionState;
        MessageBus.Current.SendMessage(new ConnectionStateChanged(this._connectionState));
        switch (this._connectionState)
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