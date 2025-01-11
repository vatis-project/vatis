using ReactiveUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Config;

public class AppConfigurationProvider : IAppConfigurationProvider
{
    private const string AppConfigurationUrl = "https://configuration.vatis.app/";
    private readonly IDownloader _downloader;
    private readonly List<string> _metarUrls;
    private AppConfiguration? _appConfiguration;

    public AppConfigurationProvider(IDownloader downloader)
    {
        _downloader = downloader;
        _metarUrls = [];
    }

    public async Task Initialize()
    {
        Log.Information("Initializing app configuration provider");
        MessageBus.Current.SendMessage(new StartupStatusChanged("Initializing app configuration provider..."));
        Log.Information($"Loading app configuration from {AppConfigurationUrl}");
        _appConfiguration =
            JsonSerializer.Deserialize(await _downloader.DownloadStringAsync(AppConfigurationUrl),
                SourceGenerationContext.NewDefault.AppConfiguration) ??
            throw new ApplicationException("Could not deserialize app configuration.");
        var vatsimStatus =
            JsonSerializer.Deserialize(await _downloader.DownloadStringAsync(_appConfiguration.VatsimStatusUrl),
                SourceGenerationContext.NewDefault.VatsimStatus) ??
            throw new ApplicationException("Deserialization of VATSIM status JSON data returned null.");
        _metarUrls.AddRange(vatsimStatus.MetarUrls);
        if (_metarUrls.Count == 0)
        {
            throw new ApplicationException("No METAR URLs found in VATSIM status data.");
        }
    }

    public string VersionUrl => _appConfiguration?.VersionUrl ?? throw new ArgumentNullException(nameof(VersionUrl));
    public string MetarUrl => _metarUrls[Random.Shared.Next(_metarUrls.Count)];
    public string NavDataUrl => _appConfiguration?.NavDataUrl ?? throw new ArgumentNullException(nameof(NavDataUrl));
    public string AtisHubUrl => _appConfiguration?.AtisHubUrl ?? throw new ArgumentNullException(nameof(AtisHubUrl));
    public string VoiceListUrl => _appConfiguration?.VoiceListUrl ?? throw new ArgumentNullException(nameof(VoiceListUrl));
    public string TextToSpeechUrl => _appConfiguration?.TextToSpeechUrl ?? throw new ArgumentNullException(nameof(TextToSpeechUrl));
    public string DigitalAtisApiUrl=> _appConfiguration?.DigitalAtisApiUrl ?? throw new ArgumentNullException(nameof(DigitalAtisApiUrl));
}
