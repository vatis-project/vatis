using System.Threading.Tasks;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather;

public interface IMetarRepository
{
    Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true);
    void RemoveMetar(string station);
}