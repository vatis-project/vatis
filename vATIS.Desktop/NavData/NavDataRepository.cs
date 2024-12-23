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
    private List<Airport> mAirports = [];
    private List<Navaid> mNavaids = [];
    private readonly IDownloader mDownloader;
    private readonly IAppConfigurationProvider mAppConfigurationProvider;

    public NavDataRepository(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        mDownloader = downloader;
        mAppConfigurationProvider = appConfigurationProvider;
    }

    public async Task CheckForUpdates()
    {
        try
        {
            MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for new navigation data..."));
            var localNavDataSerial = await GetLocalNavDataSeial();
            Log.Information($"Local NavData serial number {localNavDataSerial}");
            var response = await mDownloader.DownloadStringAsync(mAppConfigurationProvider.NavDataUrl);
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
            await mDownloader.DownloadFileAsync(availableNavData.AirportDataUrl, PathProvider.AirportsFilePath, new Progress<int>((int percent) =>
            {
                MessageBus.Current.SendMessage(new StartupStatusChanged($"Downloading airport navdata: {percent}%"));
            }));
        }

        if (!string.IsNullOrEmpty(availableNavData.NavaidDataUrl))
        {
            Log.Information($"Downloading navaid navdata from {availableNavData.NavaidDataUrl}");
            await mDownloader.DownloadFileAsync(availableNavData.NavaidDataUrl, PathProvider.NavaidsFilePath, new Progress<int>((int percent) =>
            {
                MessageBus.Current.SendMessage(new StartupStatusChanged($"Downloading navaid navdata: {percent}%"));
            }));
        }

        await File.WriteAllTextAsync(PathProvider.NavDataSerialFilePath, JsonSerializer.Serialize(availableNavData.NavDataSerial, SourceGenerationContext.NewDefault.String));
    }

    private static async Task<string> GetLocalNavDataSeial()
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
        mAirports = await Task.Run(() =>
        {
            var content = File.ReadAllText(PathProvider.AirportsFilePath);
            return JsonSerializer.Deserialize(content, SourceGenerationContext.NewDefault.ListAirport);
        }) ?? [];
    }

    private async Task LoadNavaidDatabase()
    {
        mNavaids = await Task.Run(() =>
        {
            using var source = File.OpenRead(PathProvider.NavaidsFilePath);
            using StreamReader reader = new(source);
            return JsonSerializer.Deserialize(reader.ReadToEnd(), SourceGenerationContext.NewDefault.ListNavaid);
        }) ?? [];
    }

    public Airport? GetAirport(string id)
    {
        return mAirports.FirstOrDefault(x => x.Id == id);
    }

    public Navaid? GetNavaid(string id)
    {
        return mNavaids.FirstOrDefault(x => x.Id == id);
    }
}
