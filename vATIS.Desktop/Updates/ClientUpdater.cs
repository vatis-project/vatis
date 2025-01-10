using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ReactiveUI;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Velopack;

namespace Vatsim.Vatis.Updates;

public class ClientUpdater : IClientUpdater
{
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _updateInfo;

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

        this._updateManager = new UpdateManager(
            versionUrl,
            new UpdateOptions
            {
                AllowVersionDowngrade = true
            });
    }

    public async Task<bool> Run()
    {
        MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for new client version..."));

        if (Debugger.IsAttached)
        {
            return false;
        }

        if (!this._updateManager.IsInstalled)
        {
            return false;
        }

        this._updateInfo = await this._updateManager.CheckForUpdatesAsync();

        if (this._updateInfo == null)
        {
            return false;
        }

        await this._updateManager.DownloadUpdatesAsync(this._updateInfo, ReportProgress);
        await this._updateManager.WaitExitThenApplyUpdatesAsync(this._updateInfo, true);

        return true;
    }

    private static void ReportProgress(int progress)
    {
        MessageBus.Current.SendMessage(new StartupStatusChanged($"Downloading new version: {progress}%"));
    }
}