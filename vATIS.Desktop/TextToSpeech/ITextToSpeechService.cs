using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.TextToSpeech;

public interface ITextToSpeechService
{
    List<VoiceMetaData> VoiceList { get; }

    Task Initialize();

    Task<byte[]?> RequestAudio(string text, AtisStation station, CancellationToken cancellationToken);
}