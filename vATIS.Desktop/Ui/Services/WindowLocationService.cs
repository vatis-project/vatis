using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System.Linq;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Ui.Dialogs;

namespace Vatsim.Vatis.Ui.Services;
public class WindowLocationService : IWindowLocationService
{
    private readonly IAppConfig mAppConfig;
    private int? mLeft;
    private int? mTop;

    public WindowLocationService(IAppConfig appConfig)
    {
        mAppConfig = appConfig;
    }

    public void Restore(Window? window)
    {
        if (window is null)
            return;

        if (window.GetType() == typeof(Windows.MainWindow))
        {
            if (mAppConfig.MainWindowPosition == null)
            {
                // No settings found to restore, so center the window on the primary screen.
                var primaryScreen = window.Screens.Primary;
                if (primaryScreen == null) return;
                var screenWorkingArea = primaryScreen.WorkingArea;

                var centeredLeft = (int)(screenWorkingArea.X + ((screenWorkingArea.Width - window.Width) / 2));
                var centeredTop = (int)(screenWorkingArea.Y + ((screenWorkingArea.Height - window.Height) / 2));

                mLeft = centeredLeft;
                mTop = centeredTop;

                mAppConfig.MainWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                mAppConfig.SaveConfig();
                return;
            }
            mLeft = mAppConfig.MainWindowPosition.X;
            mTop = mAppConfig.MainWindowPosition.Y;
        }
        else if (window.GetType() == typeof(Windows.CompactWindow))
        {
            if (mAppConfig.CompactWindowPosition == null)
            {
                // No settings found to restore, so center the window on the primary screen.
                var primaryScreen = window.Screens.Primary;
                if (primaryScreen != null)
                {
                    var screenWorkingArea = primaryScreen.WorkingArea;

                    var centeredLeft = (int)(screenWorkingArea.X + ((screenWorkingArea.Width - window.Width) / 2));
                    var centeredTop = (int)(screenWorkingArea.Y + ((screenWorkingArea.Height - window.Height) / 2));

                    mLeft = centeredLeft;
                    mTop = centeredTop;

                    mAppConfig.CompactWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                    mAppConfig.SaveConfig();
                }
                return;
            }
            mLeft = mAppConfig.CompactWindowPosition.X;
            mTop = mAppConfig.CompactWindowPosition.Y;
        }
        else if (window.GetType() == typeof(VoiceRecordAtisDialog))
        {
            if (mAppConfig.VoiceRecordAtisDialogWindowPosition == null)
            {
                // No settings found to restore, so center the window on the primary screen.
                var primaryScreen = window.Screens.Primary;
                if (primaryScreen == null) return;
                var screenWorkingArea = primaryScreen.WorkingArea;

                var centeredLeft = (int)(screenWorkingArea.X + ((screenWorkingArea.Width - window.Width) / 2));
                var centeredTop = (int)(screenWorkingArea.Y + ((screenWorkingArea.Height - window.Height) / 2));

                mLeft = centeredLeft;
                mTop = centeredTop;

                mAppConfig.VoiceRecordAtisDialogWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                mAppConfig.SaveConfig();
                return;
            }
            mLeft = mAppConfig.VoiceRecordAtisDialogWindowPosition.X;
            mTop = mAppConfig.VoiceRecordAtisDialogWindowPosition.Y;
        }

        if (mLeft is null || mTop is null)
            return;

        var savedPosition = new PixelPoint(mLeft.Value, mTop.Value);
        var screen = FindScreenContainingPositionInWorkingArea(window, savedPosition);
        if (screen == null)
        {
            // The saved window position (its top-left corner) is outside the working area
            // of any active screen. Therefore, keep the window's size and position at their default values.
            return;
        }
        const int minDistance = 50;
        if (mLeft.Value > screen.WorkingArea.X + screen.WorkingArea.Width - minDistance || 
            mTop.Value > screen.WorkingArea.Y + screen.WorkingArea.Height - minDistance)
        {
            // The saved top-left corner (position) is too close to the right or bottom edge
            // of the screen's working area, making the window difficult to access.
            // Therefore, keep the window's size and position at their default values.
            return;
        }

        window.Position = savedPosition;
    }

    public void Update(Window? window)
    {
        if (window is null)
            return;

        mLeft = window.Position.X;
        mTop = window.Position.Y;

        var savedPosition = new WindowPosition(mLeft.Value, mTop.Value);

        if (window.GetType() == typeof(Windows.MainWindow))
        {
            mAppConfig.MainWindowPosition = savedPosition;
            mAppConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(Windows.CompactWindow))
        {
            mAppConfig.CompactWindowPosition = savedPosition;
            mAppConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(VoiceRecordAtisDialog))
        {
            mAppConfig.VoiceRecordAtisDialogWindowPosition = savedPosition;
            mAppConfig.SaveConfig();
        }
    }

    private static Screen? FindScreenContainingPositionInWorkingArea(Window? window, PixelPoint position)
    {
        if (window is null)
            return null;

        return (from screen in window.Screens.All where screen.WorkingArea.Contains(position) select screen).FirstOrDefault();
    }
}