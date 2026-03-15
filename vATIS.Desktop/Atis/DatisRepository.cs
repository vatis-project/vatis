// <copyright file="DatisRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Networking.AtisHub.Dto;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis;

/// <inheritdoc cref="IDatisRepository"/>
public sealed class DatisRepository : IDatisRepository, IDisposable
{
    private const int UpdateIntervalSeconds = 300;
    private readonly IDownloader _downloader;
    private readonly IDatisTextProcessor _textProcessor;
    private readonly string? _digitalAtisApiUrl;
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds) };
    private readonly Dictionary<string, AtisStation> _monitoredStations = [];
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatisRepository"/> class.
    /// </summary>
    /// <param name="downloader">The downloader used to retrieve D-ATIS data.</param>
    /// <param name="appConfigurationProvider">The application configuration provider.</param>
    /// <param name="textProcessor">The text processor for D-ATIS bodies.</param>
    public DatisRepository(
        IDownloader downloader,
        IAppConfigurationProvider appConfigurationProvider,
        IDatisTextProcessor textProcessor)
    {
        _downloader = downloader;
        _textProcessor = textProcessor;
        _digitalAtisApiUrl = appConfigurationProvider.DigitalAtisApiUrl;

        _updateTimer.Tick += async (_, _) => { await UpdateAsync(); };
        _updateTimer.Start();
    }

    /// <inheritdoc />
    public void MonitorStation(AtisStation station)
    {
        _monitoredStations[station.Id] = station;
        FetchForStationAsync(station).ContinueWith(
            t => Log.Error(t.Exception, "Error during initial D-ATIS fetch for {StationId}", station.Identifier),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <inheritdoc />
    public void RemoveStation(string stationId)
    {
        _monitoredStations.Remove(stationId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _updateTimer.Stop();
            _isDisposed = true;
        }
    }

    private static DigitalAtisResponseDto? FindMatchingAtis(List<DigitalAtisResponseDto> datisList, AtisType atisType)
    {
        return atisType switch
        {
            AtisType.Combined => datisList.FirstOrDefault(a => a.AtisType == "combined")
                                 ?? datisList.FirstOrDefault(a => a.AtisType == "dep"),
            AtisType.Arrival => datisList.FirstOrDefault(a => a.AtisType == "arr"),
            AtisType.Departure => datisList.FirstOrDefault(a => a.AtisType == "dep"),
            _ => null,
        };
    }

    private static void PublishNotAvailable(AtisStation station)
    {
        EventBus.Instance.Publish(new DatisReceived(
            new DatisResult(station.Id, "D-ATIS NOT AVBL.", string.Empty, null)));
    }

    private async Task UpdateAsync()
    {
        foreach (var station in _monitoredStations.Values.ToList())
        {
            await FetchForStationAsync(station);
        }
    }

    private async Task FetchForStationAsync(AtisStation station)
    {
        try
        {
            var url = $"{_digitalAtisApiUrl}/{station.Identifier}";
            Log.Information("Fetching D-ATIS for {Station} from {Url}", station.Identifier, url);

            var response = await _downloader.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("D-ATIS request failed for {Station}: {StatusCode}", station.Identifier, response.StatusCode);
                PublishNotAvailable(station);
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var datisList = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.ListDigitalAtisResponseDto);

            if (datisList == null || datisList.Count == 0)
            {
                Log.Information("No D-ATIS data available for {Station}", station.Identifier);
                PublishNotAvailable(station);
                return;
            }

            var match = FindMatchingAtis(datisList, station.AtisType);
            if (match == null || string.IsNullOrWhiteSpace(match.Body))
            {
                Log.Information("No matching D-ATIS entry for {Station} ({AtisType})", station.Identifier, station.AtisType);
                PublishNotAvailable(station);
                return;
            }

            char? atisLetter = char.TryParse(match.AtisLetter, out var letter) ? letter : null;

            var result = _textProcessor.Process(
                match.Body,
                station.Id,
                atisLetter,
                station.Contractions,
                station.DatisTextReplacements);

            EventBus.Instance.Publish(new DatisReceived(result));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error fetching D-ATIS for {Station}", station.Identifier);
        }
    }
}
