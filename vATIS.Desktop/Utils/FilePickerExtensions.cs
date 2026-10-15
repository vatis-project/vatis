// <copyright file="FilePickerExtensions.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Vatsim.Vatis.Utils;

/// <summary>
/// Provides utility methods for working with file pickers, including opening and saving files
/// with specified configurations such as filters and titles.
/// </summary>
public static class FilePickerExtensions
{
    /// <summary>
    /// Opens a file picker dialog allowing the user to select one or more files,
    /// optionally filtering by file types and displaying a custom title.
    /// </summary>
    /// <param name="filters">
    /// A collection of file picker filters to restrict file types, or null to allow all file types.
    /// </param>
    /// <param name="title">
    /// The title of the file picker dialog, or null to use the default title.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is a list of selected file paths,
    /// or null if no files were selected.
    /// </returns>
    public static async Task<List<string>?> OpenFilePickerAsync(IReadOnlyList<FilePickerFileType>? filters = null,
        string? title = null)
    {
        var files = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true, FileTypeFilter = filters, Title = title
        });
        return files.Select(file => file.TryGetLocalPath()).OfType<string>().ToList();
    }

    /// <summary>
    /// Opens a folder picker dialog.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <returns>The selected folder path, or null if cancelled.</returns>
    public static async Task<string?> OpenFolderPickerAsync(string? title = null)
    {
        var folders = await GetStorageProvider().OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false, Title = title
        });
        return folders.Select(f => f.TryGetLocalPath()).OfType<string>().FirstOrDefault();
    }

    /// <summary>
    /// Saves a file by opening a file picker dialog configured with a specific title,
    /// file type filters, and an optional initial file name. The dialog also includes an overwrite prompt if
    /// a file with the same name already exists.
    /// </summary>
    /// <param name="title">
    /// The title displayed on the save file picker dialog.
    /// </param>
    /// <param name="filters">
    /// A collection of file picker filters to restrict the allowed file types.
    /// </param>
    /// <param name="initialFileName">
    /// The suggested initial file name for the file, or null for no initial name.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result is the selected storage file,
    /// or null if the save operation was canceled.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if the title or filters parameter is null.
    /// </exception>
    public static async Task<IStorageFile?> SaveFileAsync(string title, IReadOnlyList<FilePickerFileType> filters,
        string? initialFileName = null)
    {
        return await GetStorageProvider().SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            FileTypeChoices = filters,
            ShowOverwritePrompt = true,
            SuggestedFileName = initialFileName
        });
    }

    /// <summary>
    /// Opens a folder in the system file manager using Avalonia's launcher.
    /// </summary>
    /// <param name="path">The folder to open.</param>
    /// <returns>True if the folder was opened.</returns>
    public static async Task<bool> OpenFolderInFileManagerAsync(string path)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Launcher is not { } launcher)
        {
            return false;
        }

        try
        {
            // LaunchDirectoryInfoAsync splits paths containing spaces (e.g. "Application Support") on some
            // platforms, so launch the folder through an escaped file URI instead.
            return await launcher.LaunchUriAsync(new Uri(System.IO.Path.GetFullPath(path)));
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or System.IO.IOException or UriFormatException)
        {
            Serilog.Log.Warning(ex, "Failed to open folder {Path}", path);
            return false;
        }
    }

    private static IStorageProvider GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            throw new NullReferenceException("Missing StorageProvider instance.");
        }

        return provider;
    }
}
