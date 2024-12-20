namespace Vatsim.Vatis.Config;

public record AppConfiguration(string AtisHubUrl, string NavDataUrl, string VoiceListUrl, string TextToSpeechUrl, string VatsimStatusUrl, string VersionUrl);