using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Ui.Services;

public class WindowLocationService : IWindowLocationService
{
    private readonly IAppConfig _appConfig;
    private int? _left;
    private int? _top;

    public WindowLocationService(IAppConfig appConfig)
    {
        this._appConfig = appConfig;
    }

    public void Restore(Window? window)
    {
        if (window is null)
        {
            return;
        }

        if (window.GetType() == typeof(MainWindow))
        {
            if (this._appConfig.MainWindowPosition == null)
            {
                // No settings found to restore, so center the window on the primary screen.
                var primaryScreen = window.Screens.Primary;
                if (primaryScreen == null)
                {
                    return;
                }

                var screenWorkingArea = primaryScreen.WorkingArea;

                var centeredLeft = (int)(screenWorkingArea.X + ((screenWorkingArea.Width - window.Width) / 2));
                var centeredTop = (int)(screenWorkingArea.Y + ((screenWorkingArea.Height - window.Height) / 2));

                this._left = centeredLeft;
                this._top = centeredTop;

                this._appConfig.MainWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                this._appConfig.SaveConfig();
                return;
            }

            this._left = this._appConfig.MainWindowPosition.X;
            this._top = this._appConfig.MainWindowPosition.Y;
        }
        else if (window.GetType() == typeof(CompactWindow))
        {
            if (this._appConfig.CompactWindowPosition == null)
            {
                // No settings found to restore, so center the window on the primary screen.
                var primaryScreen = window.Screens.Primary;
                if (primaryScreen == null)
                {
                    return;
                }

                var screenWorkingArea = primaryScreen.WorkingArea;

                var centeredLeft = (int)(screenWorkingArea.X + ((screenWorkingArea.Width - window.Width) / 2));
                var centeredTop = (int)(screenWorkingArea.Y + ((screenWorkingArea.Height - window.Height) / 2));

                this._left = centeredLeft;
                this._top = centeredTop;

                this._appConfig.CompactWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                this._appConfig.SaveConfig();
                return;
            }

            this._left = this._appConfig.CompactWindowPosition.X;
            this._top = this._appConfig.CompactWindowPosition.Y;
        }
        else if (window.GetType() == typeof(ProfileListDialog))
        {
            if (this._appConfig.ProfileListDialogWindowPosition == null)
            {
                // No settings found to restore, so center the window on the primary screen.
                var primaryScreen = window.Screens.Primary;
                if (primaryScreen == null)
                {
                    return;
                }

                var screenWorkingArea = primaryScreen.WorkingArea;

                var centeredLeft = (int)(screenWorkingArea.X + ((screenWorkingArea.Width - window.Width) / 2));
                var centeredTop = (int)(screenWorkingArea.Y + ((screenWorkingArea.Height - window.Height) / 2));

                this._left = centeredLeft;
                this._top = centeredTop;

                this._appConfig.ProfileListDialogWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                this._appConfig.SaveConfig();
                return;
            }

            this._left = this._appConfig.ProfileListDialogWindowPosition.X;
            this._top = this._appConfig.ProfileListDialogWindowPosition.Y;
        }
        else if (window.GetType() == typeof(VoiceRecordAtisDialog))
        {
            if (this._appConfig.VoiceRecordAtisDialogWindowPosition == null)
            {
                // No settings found to restore, so center the window on the primary screen.
                var primaryScreen = window.Screens.Primary;
                if (primaryScreen == null)
                {
                    return;
                }

                var screenWorkingArea = primaryScreen.WorkingArea;

                var centeredLeft = (int)(screenWorkingArea.X + ((screenWorkingArea.Width - window.Width) / 2));
                var centeredTop = (int)(screenWorkingArea.Y + ((screenWorkingArea.Height - window.Height) / 2));

                this._left = centeredLeft;
                this._top = centeredTop;

                this._appConfig.VoiceRecordAtisDialogWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                this._appConfig.SaveConfig();
                return;
            }

            this._left = this._appConfig.VoiceRecordAtisDialogWindowPosition.X;
            this._top = this._appConfig.VoiceRecordAtisDialogWindowPosition.Y;
        }

        if (this._left is null || this._top is null)
        {
            return;
        }

        var savedPosition = new PixelPoint(this._left.Value, this._top.Value);
        var screen = FindScreenContainingPositionInWorkingArea(window, savedPosition);
        if (screen == null)
        {
            // The saved window position (its top-left corner) is outside the working area
            // of any active screen. Therefore, keep the window's size and position at their default values.
            return;
        }

        const int minDistance = 50;
        if (this._left.Value > screen.WorkingArea.X + screen.WorkingArea.Width - minDistance ||
            this._top.Value > screen.WorkingArea.Y + screen.WorkingArea.Height - minDistance)
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
        {
            return;
        }

        this._left = window.Position.X;
        this._top = window.Position.Y;

        var savedPosition = new WindowPosition(this._left.Value, this._top.Value);

        if (window.GetType() == typeof(MainWindow))
        {
            this._appConfig.MainWindowPosition = savedPosition;
            this._appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(CompactWindow))
        {
            this._appConfig.CompactWindowPosition = savedPosition;
            this._appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(ProfileListDialog))
        {
            this._appConfig.ProfileListDialogWindowPosition = savedPosition;
            this._appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(VoiceRecordAtisDialog))
        {
            this._appConfig.VoiceRecordAtisDialogWindowPosition = savedPosition;
            this._appConfig.SaveConfig();
        }
    }

    private static Screen? FindScreenContainingPositionInWorkingArea(Window? window, PixelPoint position)
    {
        if (window is null)
        {
            return null;
        }

        return (from screen in window.Screens.All where screen.WorkingArea.Contains(position) select screen)
            .FirstOrDefault();
    }
}