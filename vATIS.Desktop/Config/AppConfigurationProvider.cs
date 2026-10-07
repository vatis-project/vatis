// <copyright file="AppConfigurationProvider.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Config;

/// <inheritdoc />
public class AppConfigurationProvider : IAppConfigurationProvider
{
    private const string AppConfigurationUrl = "https://configuration.vatis.app/";
    private const int MaxAttempts = 3;
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);
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
        EventBus.Instance.Publish(new StartupStatusChanged("Initializing app configuration provider..."));
        Log.Information($"Loading app configuration from {AppConfigurationUrl}");
        _appConfiguration =
            JsonSerializer.Deserialize(await DownloadWithRetry(AppConfigurationUrl),
                SourceGenerationContext.NewDefault.AppConfiguration) ??
            throw new ApplicationException("Could not deserialize app configuration.");
        var vatsimStatus =
            JsonSerializer.Deserialize(await DownloadWithRetry(_appConfiguration.VatsimStatusUrl),
                SourceGenerationContext.NewDefault.VatsimStatus) ??
            throw new ApplicationException("Deserialization of VATSIM status JSON data returned null.");
        _metarUrls.AddRange(vatsimStatus.MetarUrls);
        if (_metarUrls.Count == 0)
        {
            throw new ApplicationException("No METAR URLs found in VATSIM status data.");
        }
    }

    /// <summary>
    /// Downloads a startup resource, retrying transient network failures. Each attempt is given a short deadline so a
    /// stalled connection (e.g. a DNS or connect hang on a flaky network) is abandoned and retried on a fresh
    /// connection instead of blocking startup for the full HTTP timeout.
    /// </summary>
    private async Task<string> DownloadWithRetry(string url)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await _downloader.DownloadStringAsync(url).WaitAsync(AttemptTimeout);
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is TimeoutException or HttpRequestException)
            {
                Log.Warning(ex, "Attempt {Attempt}/{Max} to download {Url} failed, retrying", attempt, MaxAttempts, url);
                await Task.Delay(RetryDelay);
            }
        }
    }
}
