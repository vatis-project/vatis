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
    private const string APP_CONFIGURATION_URL = "https://configuration.vatis.app/";
    private readonly IDownloader mDownloader;
    private readonly List<string> mMetarUrls;
    private AppConfiguration? mAppConfiguration;

    public AppConfigurationProvider(IDownloader downloader)
    {
        mDownloader = downloader;
        mMetarUrls = [];
    }

    public async Task Initialize()
    {
        Log.Information("Initializing app configuration provider");
        MessageBus.Current.SendMessage(new StartupStatusChanged("Initializing app configuration provider..."));
        Log.Information($"Loading app configuration from {APP_CONFIGURATION_URL}");
        mAppConfiguration =
            JsonSerializer.Deserialize(await mDownloader.DownloadStringAsync(APP_CONFIGURATION_URL),
                SourceGenerationContext.NewDefault.AppConfiguration) ??
            throw new ApplicationException("Could not deserialize app configuration.");
        var vatsimStatus =
            JsonSerializer.Deserialize(await mDownloader.DownloadStringAsync(mAppConfiguration.VatsimStatusUrl),
                SourceGenerationContext.NewDefault.VatsimStatus) ??
            throw new ApplicationException("Deserialization of VATSIM status JSON data returned null.");
        mMetarUrls.AddRange(vatsimStatus.MetarUrls);
        if (mMetarUrls.Count == 0)
        {
            throw new ApplicationException("No METAR URLs found in VATSIM status data.");
        }
    }

    public string VersionUrl => mAppConfiguration?.VersionUrl ?? throw new ArgumentNullException(nameof(VersionUrl));
    public string MetarUrl => mMetarUrls[Random.Shared.Next(mMetarUrls.Count)];
    public string NavDataUrl => mAppConfiguration?.NavDataUrl ?? throw new ArgumentNullException(nameof(NavDataUrl));
    public string AtisHubUrl => mAppConfiguration?.AtisHubUrl ?? throw new ArgumentNullException(nameof(AtisHubUrl));
    public string VoiceListUrl => mAppConfiguration?.VoiceListUrl ?? throw new ArgumentNullException(nameof(VoiceListUrl));
    public string TextToSpeechUrl => mAppConfiguration?.TextToSpeechUrl ?? throw new ArgumentNullException(nameof(TextToSpeechUrl));
}