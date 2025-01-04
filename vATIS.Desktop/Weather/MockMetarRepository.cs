using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

public class MockMetarRepository : IMetarRepository
{
    private readonly IDownloader mDownloader;
    private readonly Decoder.MetarDecoder mMetarDecoder;
    private const string LOCAL_METAR_SERVICE_URL = "http://localhost:5500/metar?id=";

    public MockMetarRepository(IDownloader downloader)
    {
        mDownloader = downloader;
        mMetarDecoder = new Decoder.MetarDecoder();
    }

    public async Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true)
    {
        var metar = await mDownloader.DownloadStringAsync(LOCAL_METAR_SERVICE_URL + station);
        if (!string.IsNullOrEmpty(metar))
        {
            var decodedMetar = mMetarDecoder.ParseNotStrict(metar);
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