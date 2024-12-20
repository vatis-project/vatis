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
    private readonly UpdateManager mUpdateManager;
    private UpdateInfo? mUpdateInfo;

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
        
        mUpdateManager = new UpdateManager(versionUrl, new UpdateOptions
        {
            AllowVersionDowngrade = true
        });
    }

    public async Task<bool> Run()
    {
        MessageBus.Current.SendMessage(new StartupStatusChanged("Checking for new client version..."));

        if (Debugger.IsAttached)
            return false;

        if (!mUpdateManager.IsInstalled) return false;
        mUpdateInfo = await mUpdateManager.CheckForUpdatesAsync();

        if (mUpdateInfo == null) return false;
        await mUpdateManager.DownloadUpdatesAsync(mUpdateInfo, ReportProgress);
        await mUpdateManager.WaitExitThenApplyUpdatesAsync(mUpdateInfo, silent: true);

        return true;
    }

    private static void ReportProgress(int progress)
    {
        MessageBus.Current.SendMessage(new StartupStatusChanged($"Downloading new version: {progress}%"));
    }
}
