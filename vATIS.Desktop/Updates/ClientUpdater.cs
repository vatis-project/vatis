// <copyright file="ClientUpdater.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Velopack;

namespace Vatsim.Vatis.Updates;

/// <inheritdoc />
public class ClientUpdater : IClientUpdater
{
    private readonly UpdateManager updateManager;
    private UpdateInfo? updateInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientUpdater"/> class.
    /// </summary>
    /// <param name="appConfigurationProvider">
    /// The application configuration provider used to determine the version URL based on the current operating system.
    /// </param>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown if the current operating system is not supported.
    /// </exception>
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

        this.updateManager = new UpdateManager(
            versionUrl,
            new UpdateOptions
            {
                AllowVersionDowngrade = true,
            });
    }

    /// <inheritdoc/>
    public async Task<bool> Run()
    {
        MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for new client version..."));

        if (Debugger.IsAttached)
        {
            return false;
        }

        if (!this.updateManager.IsInstalled)
        {
            return false;
        }

        this.updateInfo = await this.updateManager.CheckForUpdatesAsync();

        if (this.updateInfo == null)
        {
            return false;
        }

        await this.updateManager.DownloadUpdatesAsync(this.updateInfo, ReportProgress);
        await this.updateManager.WaitExitThenApplyUpdatesAsync(this.updateInfo, true);

        return true;
    }

    private static void ReportProgress(int progress)
    {
        MessageBus.Current.SendMessage(new StartupStatusChanged($"Downloading new version: {progress}%"));
    }
}
