// <copyright file="VatsimDataService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Networking.Vatsim;

/// <inheritdoc cref="IVatsimDataService"/>
public sealed class VatsimDataService : IVatsimDataService, IDisposable
{
    private const int ObserverFacility = 0;

    // The feed is regenerated every 15 seconds, so polling at the same rate keeps the list close to current.
    private const int UpdateIntervalSeconds = 15;

    // A local test feed is polled faster so changes to it show up quickly.
    private const int TestFeedUpdateIntervalSeconds = 3;

    private readonly IDownloader _downloader;
    private readonly IAppConfigurationProvider _appConfigurationProvider;
    private readonly DispatcherTimer _updateTimer = new() { Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds) };
    private bool _started;
    private bool _isUpdating;
    private HashSet<string>? _onlineControllers;

    /// <summary>
    /// Initializes a new instance of the <see cref="VatsimDataService"/> class.
    /// </summary>
    /// <param name="downloader">The downloader used to retrieve the data feed.</param>
    /// <param name="appConfigurationProvider">The provider of the data feed URL from the VATSIM status file.</param>
    public VatsimDataService(IDownloader downloader, IAppConfigurationProvider appConfigurationProvider)
    {
        _downloader = downloader;
        _appConfigurationProvider = appConfigurationProvider;
        _updateTimer.Tick += async (_, _) => await UpdateAsync();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string>? OnlineControllers => _onlineControllers;

    /// <summary>
    /// Extracts the callsigns of the online controllers from the raw data feed, excluding observers. ATIS connections are in a separate list of the feed.
    /// </summary>
    /// <param name="json">The data feed JSON.</param>
    /// <returns>The online controller callsigns, upper-cased.</returns>
    public static HashSet<string> ParseControllers(string json)
    {
        var feed = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.VatsimDataFeed);
        return (feed?.Controllers ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Callsign) && x.Facility != ObserverFacility)
            .Select(x => x.Callsign!.Trim().ToUpperInvariant())
            .ToHashSet();
    }

    /// <inheritdoc />
    public void Monitor()
    {
        if (_started)
        {
            return;
        }

        if (_appConfigurationProvider.VatsimDataUrls.Count == 0)
        {
            Log.Warning("The VATSIM status file did not provide a data feed URL; online-station conditions are off");
            return;
        }

        _started = true;
        if (AppConfigurationProvider.LocalDataFeedUrl != null)
        {
            _updateTimer.Interval = TimeSpan.FromSeconds(TestFeedUpdateIntervalSeconds);
        }

        _updateTimer.Start();
        Dispatcher.UIThread.Post(async () => await UpdateAsync());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _updateTimer.Stop();
    }

    private async Task<HashSet<string>> DownloadControllersAsync()
    {
        // Spread load across the feeds, and fall through to the next one if a feed fails.
        var urls = _appConfigurationProvider.VatsimDataUrls.OrderBy(_ => Random.Shared.Next()).ToList();
        for (var i = 0; ; i++)
        {
            try
            {
                return ParseControllers(await _downloader.DownloadStringAsync(urls[i]));
            }
            catch (Exception ex) when (i < urls.Count - 1)
            {
                Log.Warning(ex, "Failed to read VATSIM data feed {Url}, trying the next one", urls[i]);
            }
        }
    }

    private async Task UpdateAsync()
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            var controllers = await DownloadControllersAsync();
            var changed = _onlineControllers == null || !_onlineControllers.SetEquals(controllers);
            _onlineControllers = controllers;
            if (changed)
            {
                EventBus.Instance.Publish(new OnlineControllersUpdated());
            }
        }
        catch (Exception ex)
        {
            // Keep the last known list; conditional definitions simply stay as they were.
            Log.Warning(ex, "Failed to update the VATSIM data feed");
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
