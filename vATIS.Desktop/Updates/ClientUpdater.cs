// <copyright file="ClientUpdater.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Velopack;

namespace Vatsim.Vatis.Updates;

/// <summary>
/// Provides functionality for updating the client application.
/// </summary>
public class ClientUpdater : IClientUpdater
{
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _updateInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientUpdater"/> class.
    /// </summary>
    /// <param name="appConfigurationProvider">The application configuration provider.</param>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform is not supported.</exception>
    public ClientUpdater(IAppConfigurationProvider appConfigurationProvider)
    {
        var versionUrl = appConfigurationProvider.VersionUrl;
        if (OperatingSystem.IsMacOS())
        {
            versionUrl += "/macos/";
        }
        else if (OperatingSystem.IsLinux())
        {
            versionUrl += "/linux/";
        }
        else if (OperatingSystem.IsWindows())
        {
            versionUrl += "/windows/";
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        _updateManager = new UpdateManager(versionUrl, new UpdateOptions
        {
            AllowVersionDowngrade = true
        });
    }

    /// <inheritdoc />
    public async Task<bool> Run()
    {
        EventBus.Instance.Publish(new StartupStatusChanged("Checking for new client version..."));

        if (Debugger.IsAttached)
            return false;

        if (!_updateManager.IsInstalled) return false;
        _updateInfo = await _updateManager.CheckForUpdatesAsync();

        if (_updateInfo == null) return false;
        await _updateManager.DownloadUpdatesAsync(_updateInfo, ReportProgress);
        await _updateManager.WaitExitThenApplyUpdatesAsync(_updateInfo, silent: true);

        return true;
    }

    private static void ReportProgress(int progress)
    {
        EventBus.Instance.Publish(new StartupStatusChanged($"Downloading new version: {progress}%"));
    }
}
