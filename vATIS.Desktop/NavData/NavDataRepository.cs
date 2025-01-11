// <copyright file="NavDataRepository.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.NavData;

/// <summary>
/// Provides functionality to manage and retrieve navigation data, such as airports and navaids.
/// </summary>
public class NavDataRepository : INavDataRepository
{
    private readonly IAppConfigurationProvider appConfigurationProvider;
    private readonly IDownloader downloader;
    private List<Airport> airports = [];
    private List<Navaid> navaids = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="NavDataRepository"/> class.
    /// </summary>
    /// <param name="downloader">The downloader used to perform data retrieval operations.</param>
    /// <param name="appConfigurationProvider">The configuration provider for application settings.</param>
    public NavDataRepository(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        this.downloader = downloader;
        this.appConfigurationProvider = appConfigurationProvider;
    }

    /// <inheritdoc/>
    public async Task CheckForUpdates()
    {
        try
        {
            MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for new navigation data..."));
            var localNavDataSerial = await GetLocalNavDataSerial();
            Log.Information($"Local NavData serial number {localNavDataSerial}");
            var response = await this.downloader.DownloadStringAsync(this.appConfigurationProvider.NavDataUrl);
            {
                var availableNavData =
                    JsonSerializer.Deserialize(response, SourceGenerationContext.NewDefault.AvailableNavData);
                if (availableNavData != null)
                {
                    if (File.Exists(PathProvider.AirportsFilePath)
                        && File.Exists(PathProvider.NavaidsFilePath)
                        && availableNavData.NavDataSerial == localNavDataSerial)
                    {
                        Log.Information("NavData is up to date.");
                    }
                    else
                    {
                        await this.DownloadNavData(availableNavData);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading nav data.");
        }
    }

    /// <inheritdoc/>
    public async Task Initialize()
    {
        await Task.WhenAll(this.LoadNavaidDatabase(), this.LoadAirportDatabase());
    }

    /// <inheritdoc/>
    public Airport? GetAirport(string id)
    {
        return this.airports.FirstOrDefault(x => x.Id == id);
    }

    /// <inheritdoc/>
    public Navaid? GetNavaid(string id)
    {
        return this.navaids.FirstOrDefault(x => x.Id == id);
    }

    private static async Task<string> GetLocalNavDataSerial()
    {
        if (!File.Exists(PathProvider.NavDataSerialFilePath))
        {
            return string.Empty;
        }

        return JsonSerializer.Deserialize(
            await File.ReadAllTextAsync(PathProvider.NavDataSerialFilePath),
            SourceGenerationContext.NewDefault.String) ?? string.Empty;
    }

    private async Task DownloadNavData(AvailableNavData availableNavData)
    {
        if (!string.IsNullOrEmpty(availableNavData.AirportDataUrl))
        {
            Log.Information($"Downloading airport navdata from {availableNavData.AirportDataUrl}");
            await this.downloader.DownloadFileAsync(
                availableNavData.AirportDataUrl,
                PathProvider.AirportsFilePath,
                new Progress<int>(
                    percent =>
                    {
                        MessageBus.Current.SendMessage(
                            new StartupStatusChanged($"Downloading airport navdata: {percent}%"));
                    }));
        }

        if (!string.IsNullOrEmpty(availableNavData.NavaidDataUrl))
        {
            Log.Information($"Downloading navaid navdata from {availableNavData.NavaidDataUrl}");
            await this.downloader.DownloadFileAsync(
                availableNavData.NavaidDataUrl,
                PathProvider.NavaidsFilePath,
                new Progress<int>(
                    percent =>
                    {
                        MessageBus.Current.SendMessage(
                            new StartupStatusChanged($"Downloading navaid navdata: {percent}%"));
                    }));
        }

        await File.WriteAllTextAsync(
            PathProvider.NavDataSerialFilePath,
            JsonSerializer.Serialize(availableNavData.NavDataSerial, SourceGenerationContext.NewDefault.String));
    }

    private async Task LoadAirportDatabase()
    {
        this.airports = await Task.Run(
            () =>
            {
                using var fileStream = File.OpenRead(PathProvider.AirportsFilePath);
                return JsonSerializer.Deserialize(fileStream, SourceGenerationContext.NewDefault.ListAirport);
            }) ?? [];
    }

    private async Task LoadNavaidDatabase()
    {
        this.navaids = await Task.Run(
            () =>
            {
                using var fileStream = File.OpenRead(PathProvider.NavaidsFilePath);
                return JsonSerializer.Deserialize(fileStream, SourceGenerationContext.NewDefault.ListNavaid);
            }) ?? [];
    }
}
