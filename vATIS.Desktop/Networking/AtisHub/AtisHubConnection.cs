using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Networking.AtisHub.Dto;

namespace Vatsim.Vatis.Networking.AtisHub;

public class AtisHubConnection : IAtisHubConnection
{
    private HubConnection? _hubConnection;
    private ConnectionState _connectionState;
    private readonly IAppConfigurationProvider _appConfigurationProvider;

    public AtisHubConnection(IAppConfigurationProvider appConfigurationProvider)
    {
        _appConfigurationProvider = appConfigurationProvider;
    }

    public async Task Connect()
    {
        try
        {
            if (_hubConnection is { State: HubConnectionState.Connected })
                return;

            var serverUrl = _appConfigurationProvider.AtisHubUrl;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl)
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

        SetConnectionState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    private void SetConnectionState(ConnectionState connectionState)
    {
        _connectionState = connectionState;
        MessageBus.Current.SendMessage(new ConnectionStateChanged(_connectionState));
        switch (_connectionState)
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
