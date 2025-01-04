using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis;

public interface IAtisBuilder
{
    Task<AtisBuilderResponse> BuildAtis(AtisStation station, AtisPreset preset, char currentAtisLetter,
        DecodedMetar decodedMetar, CancellationToken cancellationToken, bool sandboxRequest = false);
    Task UpdateIds(AtisStation station, AtisPreset preset, char currentAtisLetter, CancellationToken cancellationToken);
}