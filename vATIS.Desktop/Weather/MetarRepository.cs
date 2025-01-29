// <copyright file="MetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

/// <inheritdoc cref="IMetarRepository"/>
public sealed class MetarRepository : IMetarRepository, IDisposable
{
    private const int UpdateIntervalSeconds = 300;
    private static readonly string[] s_separators = ["\r\n", "\r", "\n"];
    private readonly IDownloader _downloader;
    private readonly Decoder.MetarDecoder _metarDecoder;
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds) };
    private readonly HashSet<string> _monitoredStations = [];
    private readonly Dictionary<string, DecodedMetar> _metars = [];
    private readonly string? _metarUrl;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetarRepository"/> class.
    /// </summary>
    /// <param name="downloader">The downloader used to retrieve METAR information.</param>
    /// <param name="appConfigurationProvider">The application configuration provider containing necessary configurations.</param>
    /// <exception cref="ArgumentNullException">Thrown if any of the parameters are null.</exception>
    public MetarRepository(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        _downloader = downloader;
        _metarDecoder = new Decoder.MetarDecoder();

        _updateTimer.Tick += async (_, _) => { await UpdateAsync(); };
        _updateTimer.Start();

        _metarUrl = appConfigurationProvider.MetarUrl;
    }

    /// <inheritdoc />
    public async Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true)
    {
        if (_metars.TryGetValue(station, out var metar))
        {
            return metar;
        }

        if (monitor)
        {
            _monitoredStations.Add(station);
        }

        var rawMetar = await DownloadMetarAsync(station);
        if (string.IsNullOrWhiteSpace(rawMetar))
        {
            return null;
        }

        var parsedMetar = _metarDecoder.ParseNotStrict(rawMetar);
        if (triggerMessageBus)
        {
            EventBus.Instance.Publish(new MetarReceived(parsedMetar));
        }

        return parsedMetar;
    }

    /// <inheritdoc />
    public void RemoveMetar(string station)
    {
        _metars.Remove(station);
        _monitoredStations.Remove(station);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
    }

    private async Task UpdateAsync()
    {
        if (_monitoredStations.Count != 0)
        {
            await FetchMetarsAsync(_monitoredStations.ToList());
        }
    }

    private async Task FetchMetarsAsync(List<string> stations)
    {
        try
        {
            var url = $"{_metarUrl}?id={string.Join(',', stations)}&ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            Log.Information($"Downloading METARs from {url}");
            var array = (await _downloader.DownloadStringAsync(url)).Split(s_separators, StringSplitOptions.None);
            foreach (var rawMetar in array)
            {
                ProcessMetarResponse(rawMetar);
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Error updating METARs");
        }
    }

    private void ProcessMetarResponse(string rawMetar)
    {
        Log.Information($"Processing METAR: {rawMetar}");

        try
        {
            var metar = _metarDecoder.ParseNotStrict(rawMetar);
            _metars[metar.Icao] = metar;
            EventBus.Instance.Publish(new MetarReceived(metar));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, $"ProcessMetarResponse Failed: {rawMetar}");
        }
    }

    private async Task<string> DownloadMetarAsync(string station)
    {
        try
        {
            var url = $"{_metarUrl}?id={station}&ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            Log.Information($"Downloading METAR {station} from {url}");
            return await _downloader.DownloadStringAsync(url);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Error downloading METAR: {station}");
        }

        return "";
    }

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _updateTimer.Stop();
            }

            _isDisposed = true;
        }
    }
}
