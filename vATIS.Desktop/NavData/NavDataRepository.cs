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

public class NavDataRepository : INavDataRepository
{
    private readonly IAppConfigurationProvider _appConfigurationProvider;
    private readonly IDownloader _downloader;
    private List<Airport> _airports = [];
    private List<Navaid> _navaids = [];

    public NavDataRepository(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        this._downloader = downloader;
        this._appConfigurationProvider = appConfigurationProvider;
    }

    public async Task CheckForUpdates()
    {
        try
        {
            MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for new navigation data..."));
            var localNavDataSerial = await GetLocalNavDataSerial();
            Log.Information($"Local NavData serial number {localNavDataSerial}");
            var response = await this._downloader.DownloadStringAsync(this._appConfigurationProvider.NavDataUrl);
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

    public async Task Initialize()
    {
        await Task.WhenAll(this.LoadNavaidDatabase(), this.LoadAirportDatabase());
    }

    public Airport? GetAirport(string id)
    {
        return this._airports.FirstOrDefault(x => x.Id == id);
    }

    public Navaid? GetNavaid(string id)
    {
        return this._navaids.FirstOrDefault(x => x.Id == id);
    }

    private async Task DownloadNavData(AvailableNavData availableNavData)
    {
        if (!string.IsNullOrEmpty(availableNavData.AirportDataUrl))
        {
            Log.Information($"Downloading airport navdata from {availableNavData.AirportDataUrl}");
            await this._downloader.DownloadFileAsync(
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
            await this._downloader.DownloadFileAsync(
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

    private static async Task<string> GetLocalNavDataSerial()
    {
        if (!File.Exists(PathProvider.NavDataSerialFilePath))
        {
            return "";
        }

        return JsonSerializer.Deserialize(
            await File.ReadAllTextAsync(PathProvider.NavDataSerialFilePath),
            SourceGenerationContext.NewDefault.String) ?? "";
    }

    private async Task LoadAirportDatabase()
    {
        this._airports = await Task.Run(
            () =>
            {
                var content = File.ReadAllText(PathProvider.AirportsFilePath);
                return JsonSerializer.Deserialize(content, SourceGenerationContext.NewDefault.ListAirport);
            }) ?? [];
    }

    private async Task LoadNavaidDatabase()
    {
        this._navaids = await Task.Run(
            () =>
            {
                using var source = File.OpenRead(PathProvider.NavaidsFilePath);
                using StreamReader reader = new(source);
                return JsonSerializer.Deserialize(reader.ReadToEnd(), SourceGenerationContext.NewDefault.ListNavaid);
            }) ?? [];
    }
}