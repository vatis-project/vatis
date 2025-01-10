using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.NavData;
public class NavDataRepository : INavDataRepository
{
    private List<Airport> _airports = [];
    private List<Navaid> _navaids = [];
    private readonly IDownloader _downloader;
    private readonly IAppConfigurationProvider _appConfigurationProvider;

    public NavDataRepository(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        _downloader = downloader;
        _appConfigurationProvider = appConfigurationProvider;
    }

    public async Task CheckForUpdates()
    {
        try
        {
            MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for new navigation data..."));
            var localNavDataSerial = await GetLocalNavDataSerial();
            Log.Information($"Local NavData serial number {localNavDataSerial}");
            var response = await _downloader.DownloadStringAsync(_appConfigurationProvider.NavDataUrl);
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
                        await DownloadNavData(availableNavData);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error downloading nav data.");
        }
    }

    private async Task DownloadNavData(AvailableNavData availableNavData)
    {
        if (!string.IsNullOrEmpty(availableNavData.AirportDataUrl))
        {
            Log.Information($"Downloading airport navdata from {availableNavData.AirportDataUrl}");
            await _downloader.DownloadFileAsync(availableNavData.AirportDataUrl, PathProvider.AirportsFilePath, new Progress<int>(percent =>
            {
                MessageBus.Current.SendMessage(new StartupStatusChanged($"Downloading airport navdata: {percent}%"));
            }));
        }

        if (!string.IsNullOrEmpty(availableNavData.NavaidDataUrl))
        {
            Log.Information($"Downloading navaid navdata from {availableNavData.NavaidDataUrl}");
            await _downloader.DownloadFileAsync(availableNavData.NavaidDataUrl, PathProvider.NavaidsFilePath, new Progress<int>(percent =>
            {
                MessageBus.Current.SendMessage(new StartupStatusChanged($"Downloading navaid navdata: {percent}%"));
            }));
        }

        await File.WriteAllTextAsync(PathProvider.NavDataSerialFilePath, JsonSerializer.Serialize(availableNavData.NavDataSerial, SourceGenerationContext.NewDefault.String));
    }

    private static async Task<string> GetLocalNavDataSerial()
    {
        if (!File.Exists(PathProvider.NavDataSerialFilePath))
        {
            return "";
        }

        return JsonSerializer.Deserialize(await File.ReadAllTextAsync(PathProvider.NavDataSerialFilePath), SourceGenerationContext.NewDefault.String) ?? "";
    }

    public async Task Initialize()
    {
        await Task.WhenAll(LoadNavaidDatabase(), LoadAirportDatabase());
    }

    private async Task LoadAirportDatabase()
    {
        _airports = await Task.Run(() =>
        {
            var content = File.ReadAllText(PathProvider.AirportsFilePath);
            return JsonSerializer.Deserialize(content, SourceGenerationContext.NewDefault.ListAirport);
        }) ?? [];
    }

    private async Task LoadNavaidDatabase()
    {
        _navaids = await Task.Run(() =>
        {
            using var source = File.OpenRead(PathProvider.NavaidsFilePath);
            using StreamReader reader = new(source);
            return JsonSerializer.Deserialize(reader.ReadToEnd(), SourceGenerationContext.NewDefault.ListNavaid);
        }) ?? [];
    }

    public Airport? GetAirport(string id)
    {
        return _airports.FirstOrDefault(x => x.Id == id);
    }

    public Navaid? GetNavaid(string id)
    {
        return _navaids.FirstOrDefault(x => x.Id == id);
    }
}
