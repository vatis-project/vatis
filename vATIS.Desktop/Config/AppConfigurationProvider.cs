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
    private readonly IDownloader downloader;
    private readonly List<string> metarUrls;
    private AppConfiguration? appConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppConfigurationProvider"/> class.
    /// </summary>
    /// <param name="downloader">The downloader used to perform data retrieval operations.</param>
    public AppConfigurationProvider(IDownloader downloader)
    {
        this.downloader = downloader;
        this.metarUrls = [];
    }

    /// <inheritdoc/>
    public string VersionUrl =>
        this.appConfiguration?.VersionUrl ?? throw new ArgumentNullException(nameof(this.VersionUrl));

    /// <inheritdoc/>
    public string MetarUrl => this.metarUrls[Random.Shared.Next(this.metarUrls.Count)];

    /// <inheritdoc/>
    public string NavDataUrl =>
        this.appConfiguration?.NavDataUrl ?? throw new ArgumentNullException(nameof(this.NavDataUrl));

    /// <inheritdoc/>
    public string AtisHubUrl =>
        this.appConfiguration?.AtisHubUrl ?? throw new ArgumentNullException(nameof(this.AtisHubUrl));

    /// <inheritdoc/>
    public string VoiceListUrl => this.appConfiguration?.VoiceListUrl ??
                                  throw new ArgumentNullException(nameof(this.VoiceListUrl));

    /// <inheritdoc/>
    public string TextToSpeechUrl => this.appConfiguration?.TextToSpeechUrl ??
                                     throw new ArgumentNullException(nameof(this.TextToSpeechUrl));

    /// <inheritdoc/>
    public async Task Initialize()
    {
        Log.Information("Initializing app configuration provider");
        MessageBus.Current.SendMessage(new StartupStatusChanged("Initializing app configuration provider..."));
        Log.Information($"Loading app configuration from {AppConfigurationUrl}");
        this.appConfiguration =
            JsonSerializer.Deserialize(
                await this.downloader.DownloadStringAsync(AppConfigurationUrl),
                SourceGenerationContext.NewDefault.AppConfiguration) ??
            throw new ApplicationException("Could not deserialize app configuration.");
        var vatsimStatus =
            JsonSerializer.Deserialize(
                await this.downloader.DownloadStringAsync(this.appConfiguration.VatsimStatusUrl),
                SourceGenerationContext.NewDefault.VatsimStatus) ??
            throw new ApplicationException("Deserialization of VATSIM status JSON data returned null.");
        this.metarUrls.AddRange(vatsimStatus.MetarUrls);
        if (this.metarUrls.Count == 0)
        {
            throw new ApplicationException("No METAR URLs found in VATSIM status data.");
        }
    }
}
