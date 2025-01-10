using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ReactiveUI;
using Serilog;
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
        this._downloader = downloader;
        this._metarUrls = [];
    }

    public async Task Initialize()
    {
        Log.Information("Initializing app configuration provider");
        MessageBus.Current.SendMessage(new StartupStatusChanged("Initializing app configuration provider..."));
        Log.Information($"Loading app configuration from {AppConfigurationUrl}");
        this._appConfiguration =
            JsonSerializer.Deserialize(
                await this._downloader.DownloadStringAsync(AppConfigurationUrl),
                SourceGenerationContext.NewDefault.AppConfiguration) ??
            throw new ApplicationException("Could not deserialize app configuration.");
        var vatsimStatus =
            JsonSerializer.Deserialize(
                await this._downloader.DownloadStringAsync(this._appConfiguration.VatsimStatusUrl),
                SourceGenerationContext.NewDefault.VatsimStatus) ??
            throw new ApplicationException("Deserialization of VATSIM status JSON data returned null.");
        this._metarUrls.AddRange(vatsimStatus.MetarUrls);
        if (this._metarUrls.Count == 0)
        {
            throw new ApplicationException("No METAR URLs found in VATSIM status data.");
        }
    }

    public string VersionUrl =>
        this._appConfiguration?.VersionUrl ?? throw new ArgumentNullException(nameof(this.VersionUrl));

    public string MetarUrl => this._metarUrls[Random.Shared.Next(this._metarUrls.Count)];

    public string NavDataUrl =>
        this._appConfiguration?.NavDataUrl ?? throw new ArgumentNullException(nameof(this.NavDataUrl));

    public string AtisHubUrl =>
        this._appConfiguration?.AtisHubUrl ?? throw new ArgumentNullException(nameof(this.AtisHubUrl));

    public string VoiceListUrl => this._appConfiguration?.VoiceListUrl ??
                                  throw new ArgumentNullException(nameof(this.VoiceListUrl));

    public string TextToSpeechUrl => this._appConfiguration?.TextToSpeechUrl ??
                                     throw new ArgumentNullException(nameof(this.TextToSpeechUrl));
}