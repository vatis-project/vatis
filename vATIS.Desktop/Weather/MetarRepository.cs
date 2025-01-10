// <copyright file="MetarRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

/// <summary>
/// A repository class that manages METAR (Meteorological Aerodrome Report) data.
/// Provides functionality to retrieve, decode, and manage METAR information for specific stations.
/// </summary>
/// <remarks>
/// This class handles retrieving raw METAR data from a remote source, decoding it, and monitoring requested stations.
/// Implements functionality to interact with the METAR data, including adding, retrieving, and removing stations, and triggering message bus notifications when data is received.
/// The class is sealed to prevent inheritance and implements the <see cref="IMetarRepository"/> and <see cref="IDisposable"/> interfaces.
/// </remarks>
/// <seealso cref="IMetarRepository"/>
/// <seealso cref="IDisposable"/>
public sealed class MetarRepository : IMetarRepository, IDisposable
{
    private const int UpdateIntervalSeconds = 300;
    private static readonly string[] Separators = ["\r\n", "\r", "\n"];
    private readonly IDownloader downloader;
    private readonly MetarDecoder metarDecoder;
    private readonly Dictionary<string, DecodedMetar> metars = [];
    private readonly string? metarUrl;
    private readonly HashSet<string> monitoredStations = [];
    private readonly DispatcherTimer updateTimer = new() { Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds) };
    private bool isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetarRepository"/> class.
    /// </summary>
    /// <param name="downloader">Provides the functionality for downloading data.</param>
    /// <param name="appConfigurationProvider">Provides configuration settings for the application.</param>
    public MetarRepository(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        this.downloader = downloader;
        this.metarDecoder = new MetarDecoder();

        this.updateTimer.Tick += async (_, _) => { await this.UpdateAsync(); };
        this.updateTimer.Start();

        this.metarUrl = appConfigurationProvider.MetarUrl;
    }

    /// <summary>
    /// Disposes the resources used by the <see cref="MetarRepository"/> class.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
    }

    /// <inheritdoc/>
    public async Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true)
    {
        if (this.metars.TryGetValue(station, out var metar))
        {
            return metar;
        }

        if (monitor)
        {
            this.monitoredStations.Add(station);
        }

        var rawMetar = await this.DownloadMetarAsync(station);
        if (string.IsNullOrWhiteSpace(rawMetar))
        {
            return null;
        }

        var parsedMetar = this.metarDecoder.ParseNotStrict(rawMetar);
        if (triggerMessageBus)
        {
            MessageBus.Current.SendMessage(new MetarReceived(parsedMetar));
        }

        return parsedMetar;
    }

    /// <inheritdoc/>
    public void RemoveMetar(string station)
    {
        this.metars.Remove(station);
        this.monitoredStations.Remove(station);
    }

    private async Task UpdateAsync()
    {
        if (this.monitoredStations.Count != 0)
        {
            await this.FetchMetarsAsync(this.monitoredStations.ToList());
        }
    }

    private async Task FetchMetarsAsync(List<string> stations)
    {
        try
        {
            var url =
                $"{this.metarUrl}?id={string.Join(',', stations)}&ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            Log.Information($"Downloading METARs from {url}");
            var array = (await this.downloader.DownloadStringAsync(url)).Split(Separators, StringSplitOptions.None);
            foreach (var rawMetar in array)
            {
                this.ProcessMetarResponse(rawMetar);
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
            var metar = this.metarDecoder.ParseNotStrict(rawMetar);
            this.metars[metar.Icao] = metar;
            MessageBus.Current.SendMessage(new MetarReceived(metar));
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
            var url = $"{this.metarUrl}?id={station}&ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            Log.Information($"Downloading METAR {station} from {url}");
            return await this.downloader.DownloadStringAsync(url);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Error downloading METAR: {station}");
        }

        return string.Empty;
    }

    private void Dispose(bool disposing)
    {
        if (!this.isDisposed)
        {
            if (disposing)
            {
                this.updateTimer.Stop();
            }

            this.isDisposed = true;
        }
    }
}
