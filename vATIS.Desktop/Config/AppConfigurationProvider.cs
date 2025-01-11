// <copyright file="AppConfigurationProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Config;

/// <inheritdoc />
public class AppConfigurationProvider : IAppConfigurationProvider
{
    private const string AppConfigurationUrl = "https://configuration.vatis.app/";
    private readonly IDownloader _downloader;
    private readonly List<string> _metarUrls;
    private AppConfiguration? _appConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppConfigurationProvider"/> class.
    /// </summary>
    /// <param name="downloader">An instance of <see cref="IDownloader"/> to handle download operations.</param>
    public AppConfigurationProvider(IDownloader downloader)
    {
        _downloader = downloader;
        _metarUrls = [];
    }

    /// <inheritdoc />
    public string VersionUrl => _appConfiguration?.VersionUrl ?? throw new ArgumentNullException(nameof(VersionUrl));

    /// <inheritdoc />
    public string MetarUrl => _metarUrls[Random.Shared.Next(_metarUrls.Count)];

    /// <inheritdoc />
    public string NavDataUrl => _appConfiguration?.NavDataUrl ?? throw new ArgumentNullException(nameof(NavDataUrl));

    /// <inheritdoc />
    public string AtisHubUrl => _appConfiguration?.AtisHubUrl ?? throw new ArgumentNullException(nameof(AtisHubUrl));

    /// <inheritdoc />
    public string VoiceListUrl =>
        _appConfiguration?.VoiceListUrl ?? throw new ArgumentNullException(nameof(VoiceListUrl));

    /// <inheritdoc />
    public string TextToSpeechUrl =>
        _appConfiguration?.TextToSpeechUrl ?? throw new ArgumentNullException(nameof(TextToSpeechUrl));

    /// <inheritdoc />
    public string DigitalAtisApiUrl =>
        _appConfiguration?.DigitalAtisApiUrl ?? throw new ArgumentNullException(nameof(DigitalAtisApiUrl));

    /// <inheritdoc />
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
}
