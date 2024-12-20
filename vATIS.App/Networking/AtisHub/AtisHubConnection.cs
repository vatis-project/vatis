using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Networking.AtisHub;

public class AtisHubConnection : IAtisHubConnection
{
    private HubConnection? mHubConnection;
    private ConnectionState mConnectionState;
    private readonly IAppConfigurationProvider mAppConfigurationProvider;

    public AtisHubConnection(IAppConfigurationProvider appConfigurationProvider)
    {
        mAppConfigurationProvider = appConfigurationProvider;
    }

    public async Task Connect()
    {
        try
        {
            if (mHubConnection is { State: HubConnectionState.Connected })
                return;

            var serverUrl = mAppConfigurationProvider.AtisHubUrl;
            
            mHubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl)
                .WithAutomaticReconnect()
                .AddJsonProtocol(options =>
                {
                    options.PayloadSerializerOptions.TypeInfoResolverChain.Add(SourceGenerationContext.NewDefault);
                })
                .Build();
            
            mHubConnection.Closed += OnHubConnectionClosed;
            mHubConnection.On<List<AtisHubDto>>("AtisReceived", (dtoList) =>
            {
                foreach (var dto in dtoList)
                {
                    MessageBus.Current.SendMessage(new AtisHubAtisReceived(dto));
                }
            });
            mHubConnection.On<AtisHubDto>("RemoveAtisReceived", (dto) =>
            {
                MessageBus.Current.SendMessage(new AtisHubExpiredAtisReceived(dto));
            });

            SetConnectionState(ConnectionState.Connecting);
            Log.Information($"Connecting to AtisHub server: {serverUrl}");
            await mHubConnection.StartAsync();
            Log.Information("Connected to AtisHub with ID: " + mHubConnection.ConnectionId);
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
        if (mHubConnection == null)
            return;

        try
        {
            await mHubConnection.StopAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message, "Failed to disconnect from AtisHub.");
        }
    }

    public async Task PublishAtis(AtisHubDto dto)
    {
        if (mHubConnection is not { State: HubConnectionState.Connected })
            return;

        await mHubConnection.InvokeAsync("PublishAtis", dto);
    }

    public async Task SubscribeToAtis(SubscribeDto dto)
    {
        if (mHubConnection is not { State: HubConnectionState.Connected })
            return;

        await mHubConnection.InvokeAsync("SubscribeToAtis", dto);
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
        mConnectionState = connectionState;
        MessageBus.Current.SendMessage(new ConnectionStateChanged(mConnectionState));
        switch (mConnectionState)
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