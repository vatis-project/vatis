// <copyright file="AppDataShortcut.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Serilog;
using Vatsim.Vatis.Io;

namespace Vatsim.Vatis.Ui.Extensions;

/// <summary>
/// Provides the keyboard shortcut for opening the application data folder.
/// </summary>
public static class AppDataShortcut
{
    /// <summary>
    /// Handles Ctrl+Shift+O (Cmd+Shift+O on macOS) by opening the application data folder.
    /// </summary>
    /// <param name="window">The window that received the key press.</param>
    /// <param name="e">The key event arguments.</param>
    public static async void HandleKeyDown(Window window, KeyEventArgs e)
    {
        if (e.Key != Key.O || (e.KeyModifiers != (KeyModifiers.Control | KeyModifiers.Shift) &&
                               e.KeyModifiers != (KeyModifiers.Meta | KeyModifiers.Shift)))
        {
            return;
        }

        e.Handled = true;

        try
        {
            var folder = await window.StorageProvider.TryGetFolderFromPathAsync(new Uri(PathProvider.AppDataFolderPath));
            if (folder != null)
            {
                await window.Launcher.LaunchUriAsync(folder.Path);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open application data folder");
        }
    }
}
