// <copyright file="MockAtisHubConnection.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Networking.AtisHub.Dto;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;

namespace Vatsim.Vatis.Networking.AtisHub;

/// <summary>
/// A mock implementation of the <see cref="MockAtisHubConnection"/> class used for connecting to and interacting with the ATIS Hub.
/// </summary>
public class MockAtisHubConnection : IAtisHubConnection
{
    private readonly IDownloader _downloader;
    private readonly IAppConfigurationProvider _appConfigurationProvider;
    private readonly MetarDecoder _metarDecoder;
    private HubConnection? _hubConnection;
    private ConnectionState _hubConnectionState;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockAtisHubConnection"/> class.
    /// </summary>
    /// <param name="downloader">An instance of <see cref="IDownloader"/> to handle download operations.</param>
    /// <param name="appConfigurationProvider">An instance of <see cref="IAppConfigurationProvider"/> to provide application configuration settings.</param>
    public MockAtisHubConnection(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        _downloader = downloader;
        _appConfigurationProvider = appConfigurationProvider;
        _metarDecoder = new MetarDecoder();
    }

    /// <inheritdoc />
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
                    var metar = _metarDecoder.ParseNotStrict(message);
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
            Log.Error(ex.Message, "Failed to disconnect from Dev AtisHub.");
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
        if (string.IsNullOrEmpty(dto.Id))
            return null;

        var response = await _downloader.GetAsync(_appConfigurationProvider.DigitalAtisApiUrl + "/" + dto.Id);
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

        return null;
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
