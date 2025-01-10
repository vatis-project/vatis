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
/// Provides extension methods for handling file picker operations such as selecting and saving files.
/// </summary>
public static class FilePickerExtensions
{
    /// <summary>
    /// Opens a file picker dialog to allow the user to select multiple files.
    /// </summary>
    /// <param name="filters">An optional list of file type filters to restrict selectable files.</param>
    /// <param name="title">An optional title to be displayed on the file picker dialog.</param>
    /// <returns>
    /// A list of local file paths selected by the user, or null if no files were selected.
    /// </returns>
    public static async Task<List<string>?> OpenFilePickerAsync(
        IReadOnlyList<FilePickerFileType>? filters = null,
        string? title = null)
    {
        var files = await GetStorageProvider().OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = true, FileTypeFilter = filters, Title = title,
            });
        return files.Select(file => file.TryGetLocalPath()).OfType<string>().ToList();
    }

    /// <summary>
    /// Opens a save file dialog to allow the user to specify a location and name for saving a file.
    /// </summary>
    /// <param name="title">The title to be displayed on the save file dialog.</param>
    /// <param name="filters">A list of file type filters to limit the file format options shown to the user.</param>
    /// <param name="initialFileName">An optional suggested file name for the save file dialog.</param>
    /// <returns>
    /// The saved file as an IStorageFile, or null if the save operation was canceled by the user.
    /// </returns>
    public static async Task<IStorageFile?> SaveFileAsync(
        string title,
        IReadOnlyList<FilePickerFileType> filters,
        string? initialFileName = null)
    {
        return await GetStorageProvider().SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = title,
                FileTypeChoices = filters,
                ShowOverwritePrompt = true,
                SuggestedFileName = initialFileName,
            });
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
