using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis;

public interface IAtisBuilder
{
    Task<AtisBuilderResponse> BuildAtis(AtisStation station, AtisPreset preset, char currentAtisLetter,
        object? stationMetar, CancellationToken cancellationToken, bool sandboxRequest = false);
    Task UpdateIds(AtisStation station, AtisPreset preset, char currentAtisLetter, CancellationToken cancellationToken);
}