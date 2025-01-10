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

        _updateManager = new UpdateManager(versionUrl, new UpdateOptions
        {
            AllowVersionDowngrade = true
        });
    }

    public async Task<bool> Run()
    {
        MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for new client version..."));

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
        MessageBus.Current.SendMessage(new StartupStatusChanged($"Downloading new version: {progress}%"));
    }
}
