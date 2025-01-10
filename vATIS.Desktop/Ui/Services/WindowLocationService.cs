// <copyright file="WindowLocationService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Profiles;
using Vatsim.Vatis.Ui.Windows;

namespace Vatsim.Vatis.Ui.Services;

/// <summary>
/// Provides services for storing and restoring the location of application windows.
/// </summary>
public class WindowLocationService : IWindowLocationService
{
    private readonly IAppConfig appConfig;
    private int? left;
    private int? top;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowLocationService"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration instance.</param>
    public WindowLocationService(IAppConfig appConfig)
    {
        this.appConfig = appConfig;
    }

    /// <inheritdoc/>
    public void Restore(Window? window)
    {
        if (window is null)
        {
            return;
        }

        if (window.GetType() == typeof(MainWindow))
        {
            if (this.appConfig.MainWindowPosition == null)
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

                this.left = centeredLeft;
                this.top = centeredTop;

                this.appConfig.MainWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                this.appConfig.SaveConfig();
                return;
            }

            this.left = this.appConfig.MainWindowPosition.X;
            this.top = this.appConfig.MainWindowPosition.Y;
        }
        else if (window.GetType() == typeof(CompactWindow))
        {
            if (this.appConfig.CompactWindowPosition == null)
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

                this.left = centeredLeft;
                this.top = centeredTop;

                this.appConfig.CompactWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                this.appConfig.SaveConfig();
                return;
            }

            this.left = this.appConfig.CompactWindowPosition.X;
            this.top = this.appConfig.CompactWindowPosition.Y;
        }
        else if (window.GetType() == typeof(ProfileListDialog))
        {
            if (this.appConfig.ProfileListDialogWindowPosition == null)
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

                this.left = centeredLeft;
                this.top = centeredTop;

                this.appConfig.ProfileListDialogWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                this.appConfig.SaveConfig();
                return;
            }

            this.left = this.appConfig.ProfileListDialogWindowPosition.X;
            this.top = this.appConfig.ProfileListDialogWindowPosition.Y;
        }
        else if (window.GetType() == typeof(VoiceRecordAtisDialog))
        {
            if (this.appConfig.VoiceRecordAtisDialogWindowPosition == null)
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

                this.left = centeredLeft;
                this.top = centeredTop;

                this.appConfig.VoiceRecordAtisDialogWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                this.appConfig.SaveConfig();
                return;
            }

            this.left = this.appConfig.VoiceRecordAtisDialogWindowPosition.X;
            this.top = this.appConfig.VoiceRecordAtisDialogWindowPosition.Y;
        }

        if (this.left is null || this.top is null)
        {
            return;
        }

        var savedPosition = new PixelPoint(this.left.Value, this.top.Value);
        var screen = FindScreenContainingPositionInWorkingArea(window, savedPosition);
        if (screen == null)
        {
            // The saved window position (its top-left corner) is outside the working area
            // of any active screen. Therefore, keep the window's size and position at their default values.
            return;
        }

        const int minDistance = 50;
        if (this.left.Value > screen.WorkingArea.X + screen.WorkingArea.Width - minDistance ||
            this.top.Value > screen.WorkingArea.Y + screen.WorkingArea.Height - minDistance)
        {
            // The saved top-left corner (position) is too close to the right or bottom edge
            // of the screen's working area, making the window difficult to access.
            // Therefore, keep the window's size and position at their default values.
            return;
        }

        window.Position = savedPosition;
    }

    /// <inheritdoc/>
    public void Update(Window? window)
    {
        if (window is null)
        {
            return;
        }

        this.left = window.Position.X;
        this.top = window.Position.Y;

        var savedPosition = new WindowPosition(this.left.Value, this.top.Value);

        if (window.GetType() == typeof(MainWindow))
        {
            this.appConfig.MainWindowPosition = savedPosition;
            this.appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(CompactWindow))
        {
            this.appConfig.CompactWindowPosition = savedPosition;
            this.appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(ProfileListDialog))
        {
            this.appConfig.ProfileListDialogWindowPosition = savedPosition;
            this.appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(VoiceRecordAtisDialog))
        {
            this.appConfig.VoiceRecordAtisDialogWindowPosition = savedPosition;
            this.appConfig.SaveConfig();
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
