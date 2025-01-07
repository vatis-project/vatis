using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;

namespace Vatsim.Vatis.Networking.AtisHub;

public class MockAtisHubConnection : IAtisHubConnection
{
    private readonly IDownloader _downloader;
    private HubConnection? mHubConnection;
    private ConnectionState mConnectionState;

    public MockAtisHubConnection(IDownloader downloader)
    {
        _downloader = downloader;
    }
    
    public async Task Connect()
    {
        try
        {
            if (mHubConnection is { State: HubConnectionState.Connected })
                return;

            mHubConnection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5500/hub")
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
            mHubConnection.On<string>("MetarReceived", (message) =>
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
            await mHubConnection.StartAsync();
            Log.Information("Connected to Dev AtisHub with ID: " + mHubConnection.ConnectionId);
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
            Log.Error(ex.Message, "Failed to disconnect from Dev AtisHub.");
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

    public async Task<char> GetDigitalAtisLetter(DigitalAtisRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.Id))
            return '\0';

        var response = await _downloader.GetAsync("https://datis.clowd.io/api/" + dto.Id);
        if (response.IsSuccessStatusCode)
        {
            var json = JsonSerializer.Deserialize(await response.Content.ReadAsStringAsync(),
                SourceGenerationContext.NewDefault.ListDigitalAtisResponseDto);
            if (json != null)
            {
                foreach (var atis in json)
                {
                    // user only has combined ATIS configured
                    if (dto.AtisType == AtisType.Combined)
                    {
                        if (atis.AtisType == "dep")
                        {
                            if (char.TryParse(atis.AtisLetter, out var atisLetter))
                            {
                                return atisLetter;
                            }
                        }
                    }

                    if (atis.AtisType == "arr")
                    {
                        if (dto.AtisType == AtisType.Arrival)
                        {
                            if (char.TryParse(atis.AtisLetter, out var atisLetter))
                            {
                                return atisLetter;
                            }
                        }
                    }
                    else if (atis.AtisType == "dep")
                    {
                        if (dto.AtisType == AtisType.Departure)
                        {
                            if (char.TryParse(atis.AtisLetter, out var atisLetter))
                            {
                                return atisLetter;
                            }
                        }
                    }
                    else
                    {
                        if (char.TryParse(atis.AtisLetter, out var atisLetter))
                        {
                            return atisLetter;
                        }
                    }
                }
            }
        }

        return '\0';
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