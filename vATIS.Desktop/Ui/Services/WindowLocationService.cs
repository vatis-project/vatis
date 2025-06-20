﻿// <copyright file="WindowLocationService.cs" company="Justin Shannon">
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
    private readonly IAppConfig _appConfig;
    private int? _left;
    private int? _top;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowLocationService"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration instance.</param>
    public WindowLocationService(IAppConfig appConfig)
    {
        _appConfig = appConfig;
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
            if (_appConfig.MainWindowPosition == null)
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

                _left = centeredLeft;
                _top = centeredTop;

                _appConfig.MainWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                _appConfig.SaveConfig();
                return;
            }

            _left = _appConfig.MainWindowPosition.X;
            _top = _appConfig.MainWindowPosition.Y;
        }
        else if (window.GetType() == typeof(MiniWindow))
        {
            if (_appConfig.MiniWindowPosition == null)
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

                _left = centeredLeft;
                _top = centeredTop;

                _appConfig.MiniWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                _appConfig.SaveConfig();
                return;
            }

            _left = _appConfig.MiniWindowPosition.X;
            _top = _appConfig.MiniWindowPosition.Y;
        }
        else if (window.GetType() == typeof(ProfileListDialog))
        {
            if (_appConfig.ProfileListDialogWindowPosition == null)
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

                _left = centeredLeft;
                _top = centeredTop;

                _appConfig.ProfileListDialogWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                _appConfig.SaveConfig();
                return;
            }

            _left = _appConfig.ProfileListDialogWindowPosition.X;
            _top = _appConfig.ProfileListDialogWindowPosition.Y;
        }
        else if (window.GetType() == typeof(VoiceRecordAtisDialog))
        {
            if (_appConfig.VoiceRecordAtisDialogWindowPosition == null)
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

                _left = centeredLeft;
                _top = centeredTop;

                _appConfig.VoiceRecordAtisDialogWindowPosition = new WindowPosition(centeredLeft, centeredTop);
                _appConfig.SaveConfig();
                return;
            }

            _left = _appConfig.VoiceRecordAtisDialogWindowPosition.X;
            _top = _appConfig.VoiceRecordAtisDialogWindowPosition.Y;
        }

        if (_left is null || _top is null)
        {
            return;
        }

        var savedPosition = new PixelPoint(_left.Value, _top.Value);
        var screen = FindScreenContainingPositionInWorkingArea(window, savedPosition);
        if (screen == null)
        {
            // The saved window position (its top-left corner) is outside the working area
            // of any active screen. Therefore, keep the window's size and position at their default values.
            return;
        }

        const int minDistance = 50;
        if (_left.Value > screen.WorkingArea.X + screen.WorkingArea.Width - minDistance ||
            _top.Value > screen.WorkingArea.Y + screen.WorkingArea.Height - minDistance)
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

        _left = window.Position.X;
        _top = window.Position.Y;

        var savedPosition = new WindowPosition(_left.Value, _top.Value);

        if (window.GetType() == typeof(MainWindow))
        {
            _appConfig.MainWindowPosition = savedPosition;
            _appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(MiniWindow))
        {
            _appConfig.MiniWindowPosition = savedPosition;
            _appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(ProfileListDialog))
        {
            _appConfig.ProfileListDialogWindowPosition = savedPosition;
            _appConfig.SaveConfig();
        }
        else if (window.GetType() == typeof(VoiceRecordAtisDialog))
        {
            _appConfig.VoiceRecordAtisDialogWindowPosition = savedPosition;
            _appConfig.SaveConfig();
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
