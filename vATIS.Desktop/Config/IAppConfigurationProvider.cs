using System.Threading.Tasks;

namespace Vatsim.Vatis.Config;

public interface IAppConfigurationProvider
{
    string VersionUrl { get; }

    string MetarUrl { get; }

    string NavDataUrl { get; }

    string AtisHubUrl { get; }

    string VoiceListUrl { get; }

    string TextToSpeechUrl { get; }

    Task Initialize();
}