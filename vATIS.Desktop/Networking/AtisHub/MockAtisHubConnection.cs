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

public class MockAtisHubConnection : IAtisHubConnection
{
    private HubConnection? _hubConnection;
    private ConnectionState _hubConnectionState;

    public async Task Connect()
    {
        try
        {
            if (this._hubConnection is { State: HubConnectionState.Connected })
            {
                return;
            }

            this._hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://{IPAddress.Loopback.ToString()}:5500/hub")
                .WithAutomaticReconnect()
                .AddJsonProtocol(
                    options =>
                    {
                        options.PayloadSerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.NewDefault);
                    })
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
            this._hubConnection.On<string>(
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
            await this._hubConnection.StartAsync();
            Log.Information("Connected to Dev AtisHub with ID: " + this._hubConnection.ConnectionId);
            this.SetConnectionState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            this.SetConnectionState(ConnectionState.Disconnected);
            Log.Error(ex.Message, "Failed to connect to Dev AtisHub.");
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
            Log.Error(ex.Message, "Failed to disconnect from Dev AtisHub.");
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

    private void SetConnectionState(ConnectionState connectionState)
    {
        this._hubConnectionState = connectionState;
        MessageBus.Current.SendMessage(new ConnectionStateChanged(this._hubConnectionState));
        switch (this._hubConnectionState)
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