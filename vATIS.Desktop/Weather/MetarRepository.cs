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
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

public sealed class MetarRepository : IMetarRepository, IDisposable
{
    private const int UPDATE_INTERVAL_SECONDS = 300;
    private readonly IDownloader mDownloader;
    private readonly Decoder.MetarDecoder mMetarDecoder;
    private readonly DispatcherTimer mUpdateTimer = new() { Interval = TimeSpan.FromSeconds(UPDATE_INTERVAL_SECONDS) };
    private readonly HashSet<string> mMonitoredStations = [];
    private readonly Dictionary<string, DecodedMetar> mMetars = [];
    private static readonly string[] Separators = ["\r\n", "\r", "\n"];
    private readonly string? mMetarUrl;
    private bool mIsDisposed;

    public MetarRepository(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        mDownloader = downloader;
        mMetarDecoder = new Decoder.MetarDecoder();

        mUpdateTimer.Tick += async delegate { await Update(); };
        mUpdateTimer.Start();

        mMetarUrl = appConfigurationProvider.MetarUrl;
    }

    private async Task Update()
    {
        if (mMonitoredStations.Count != 0)
        {
            await FetchMetars(mMonitoredStations.ToList());
        }
    }

    private async Task FetchMetars(List<string> stations)
    {
        try
        {
            var url = $"{mMetarUrl}?id={string.Join(',', stations)}&ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            Log.Information($"Downloading METARs from {url}");
            var array = (await mDownloader.DownloadStringAsync(url)).Split(Separators, StringSplitOptions.None);
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
            var metar = mMetarDecoder.ParseNotStrict(rawMetar);
            mMetars[metar.Icao] = metar;
            MessageBus.Current.SendMessage(new MetarReceived(metar));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, $"ProcessMetarResponse Failed: {rawMetar}");
        }
    }
    
    private async Task<string> DownloadMetar(string station)
    {
        try
        {
            var url = $"{mMetarUrl}?id={station}&ts={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            Log.Information($"Downloading METAR {station} from {url}");
            return await mDownloader.DownloadStringAsync(url);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, $"Error downloading METAR: {station}");
        }

        return "";
    }

    public async Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true)
    {
        if (mMetars.TryGetValue(station, out var metar))
        {
            return metar;
        }

        if (monitor)
        {
            mMonitoredStations.Add(station);
        }

        var rawMetar = await DownloadMetar(station);
        if (string.IsNullOrWhiteSpace(rawMetar))
        {
            return null;
        }

        var parsedMetar = mMetarDecoder.ParseNotStrict(rawMetar);
        if (triggerMessageBus)
        {
            MessageBus.Current.SendMessage(new MetarReceived(parsedMetar));
        }

        return parsedMetar;
    }

    public void RemoveMetar(string station)
    {
        mMetars.Remove(station);
        mMonitoredStations.Remove(station);
    }

    private void Dispose(bool disposing)
    {
        if (!mIsDisposed)
        {
            if (disposing)
            {
                mUpdateTimer.Stop();
            }

            mIsDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
    }
}