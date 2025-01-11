namespace Vatsim.Vatis.Config;

public record AppConfiguration(
    string AtisHubUrl,
    string DigitalAtisApiUrl,
    string NavDataUrl,
    string TextToSpeechUrl,
    string VatsimStatusUrl,
    string VersionUrl,
    string VoiceListUrl);
