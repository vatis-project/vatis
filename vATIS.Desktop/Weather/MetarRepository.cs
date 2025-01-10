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
    private const int UpdateIntervalSeconds = 300;
    private readonly IDownloader _downloader;
    private readonly Decoder.MetarDecoder _metarDecoder;
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds) };
    private readonly HashSet<string> _monitoredStations = [];
    private readonly Dictionary<string, DecodedMetar> _metars = [];
    private static readonly string[] s_separators = ["\r\n", "\r", "\n"];
    private readonly string? _metarUrl;
    private bool _isDisposed;

    public MetarRepository(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        _downloader = downloader;
        _metarDecoder = new Decoder.MetarDecoder();

        _updateTimer.Tick += async delegate { await UpdateAsync(); };
        _updateTimer.Start();

        _metarUrl = appConfigurationProvider.MetarUrl;
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
            MessageBus.Current.SendMessage(new MetarReceived(parsedMetar));
        }

        return parsedMetar;
    }

    public void RemoveMetar(string station)
    {
        _metars.Remove(station);
        _monitoredStations.Remove(station);
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

    public void Dispose()
    {
        Dispose(disposing: true);
    }
}
