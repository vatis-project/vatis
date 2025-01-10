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
            if (_hubConnection is { State: HubConnectionState.Connected })
                return;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://{IPAddress.Loopback.ToString()}:5500/hub")
                .WithAutomaticReconnect()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.NewDefault);
                })
                .Build();

            _hubConnection.Closed += OnHubConnectionClosed;
            _hubConnection.On<List<AtisHubDto>>("AtisReceived", (dtoList) =>
            {
                foreach (var dto in dtoList)
                {
                    MessageBus.Current.SendMessage(new AtisHubAtisReceived(dto));
                }
            });
            _hubConnection.On<AtisHubDto>("RemoveAtisReceived", (dto) =>
            {
                MessageBus.Current.SendMessage(new AtisHubExpiredAtisReceived(dto));
            });
            _hubConnection.On<string>("MetarReceived", (message) =>
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

            SetConnectionState(ConnectionState.Connecting);
            Log.Information($"Connecting to Dev AtisHub.");
            await _hubConnection.StartAsync();
            Log.Information("Connected to Dev AtisHub with ID: " + _hubConnection.ConnectionId);
            SetConnectionState(ConnectionState.Connected);
        }
        catch (Exception ex)
        {
            SetConnectionState(ConnectionState.Disconnected);
            Log.Error(ex.Message, "Failed to connect to Dev AtisHub.");
        }
    }

    private void SetConnectionState(ConnectionState connectionState)
    {
        _hubConnectionState = connectionState;
        MessageBus.Current.SendMessage(new ConnectionStateChanged(_hubConnectionState));
        switch (_hubConnectionState)
        {
            case ConnectionState.Connected:
                MessageBus.Current.SendMessage(new HubConnected());
                break;
            case ConnectionState.Disconnected:
                MessageBus.Current.SendMessage(new HubDisconnected());
                break;
        }
    }

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
            Log.Error(ex.Message, "Failed to disconnect from Dev AtisHub.");
        }
    }

    public async Task PublishAtis(AtisHubDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return;

        await _hubConnection.InvokeAsync("PublishAtis", dto);
    }

    public async Task SubscribeToAtis(SubscribeDto dto)
    {
        if (_hubConnection is not { State: HubConnectionState.Connected })
            return;

        await _hubConnection.InvokeAsync("SubscribeToAtis", dto);
    }

    private Task OnHubConnectionClosed(Exception? exception)
    {
        if (exception != null)
        {
            Log.Error(exception, "Dev AtisHub connection closed unexpectedly.");
        }

        SetConnectionState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }
}
