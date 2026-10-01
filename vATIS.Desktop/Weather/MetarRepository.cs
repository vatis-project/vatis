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
    private readonly Dictionary<string, string> _customUrls = [];
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
    public async Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true,
        string? customUrl = null)
    {
        // A cached METAR may have come from the default source, so skip it when a custom URL is
        // requested unless this station is already being monitored with that same URL.
        var useCache = string.IsNullOrWhiteSpace(customUrl)
                       || (_customUrls.TryGetValue(station, out var monitoredUrl) && monitoredUrl == customUrl.Trim());
        if (useCache && _metars.TryGetValue(station, out var metar))
        {
            return metar;
        }

        if (monitor)
        {
            _monitoredStations.Add(station);
            if (string.IsNullOrWhiteSpace(customUrl))
            {
                _customUrls.Remove(station);
            }
            else
            {
                _customUrls[station] = customUrl.Trim();
            }
        }

        if (!string.IsNullOrWhiteSpace(customUrl))
        {
            var customMetar = await FetchCustomMetarAsync(station, customUrl.Trim());
            if (customMetar != null)
            {
                if (triggerMessageBus)
                {
                    EventBus.Instance.Publish(new MetarReceived(customMetar));
                }

                return customMetar;
            }
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
        _customUrls.Remove(station);
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
            var customStations = _monitoredStations.Where(_customUrls.ContainsKey).ToList();
            var standardStations = _monitoredStations.Except(customStations).ToList();

            foreach (var station in customStations)
            {
                var metar = await FetchCustomMetarAsync(station, _customUrls[station]);
                if (metar != null)
                {
                    _metars[station] = metar;
                    EventBus.Instance.Publish(new MetarReceived(metar));
                }
                else
                {
                    // Custom source unavailable, fall back to the VATSIM source.
                    standardStations.Add(station);
                }
            }

            if (standardStations.Count != 0)
            {
                await FetchMetarsAsync(standardStations);
            }
        }
    }

    /// <summary>
    /// Downloads a METAR from a user-supplied URL. The URL may contain an {icao} placeholder.
    /// </summary>
    /// <returns>The decoded METAR, or null if the source failed or returned no usable METAR.</returns>
    private async Task<DecodedMetar?> FetchCustomMetarAsync(string station, string urlTemplate)
    {
        try
        {
            var url = urlTemplate.Replace("{icao}", station, StringComparison.OrdinalIgnoreCase);
            Log.Information($"Downloading METAR {station} from custom source {url}");
            var response = await _downloader.DownloadStringAsync(url);

            foreach (var line in response.Split(s_separators, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                var metar = _metarDecoder.ParseNotStrict(trimmed);
                if (string.Equals(metar.Icao, station, StringComparison.OrdinalIgnoreCase))
                {
                    return metar;
                }
            }

            Log.Warning($"Custom METAR source for {station} returned no matching METAR, using default source");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Error downloading METAR from custom source: {station}");
        }

        return null;
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
