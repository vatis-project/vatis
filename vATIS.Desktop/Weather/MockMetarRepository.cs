using System.Net;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

public class MockMetarRepository : IMetarRepository
{
    private readonly IDownloader _downloader;
    private readonly Decoder.MetarDecoder _metarDecoder;
    private readonly string _localMetarServiceUrl = $"http://{IPAddress.Loopback.ToString()}:5500/metar?id=";

    public MockMetarRepository(IDownloader downloader)
    {
        _downloader = downloader;
        _metarDecoder = new Decoder.MetarDecoder();
    }

    public async Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true)
    {
        var metar = await _downloader.DownloadStringAsync(_localMetarServiceUrl + station);
        if (!string.IsNullOrEmpty(metar))
        {
            var decodedMetar = _metarDecoder.ParseNotStrict(metar);
            MessageBus.Current.SendMessage(new MetarReceived(decodedMetar));
            return decodedMetar;
        }

        return null;
    }

    public void RemoveMetar(string station)
    {
        // Ignore
    }
}
