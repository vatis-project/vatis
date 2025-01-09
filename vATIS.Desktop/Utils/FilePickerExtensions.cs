using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Vatsim.Vatis.Utils;

public static class FilePickerExtensions
{
    private static IStorageProvider GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            throw new NullReferenceException("Missing StorageProvider instance.");
        }

        return provider;
    }

    public static async Task<List<string>?> OpenFilePickerAsync(IReadOnlyList<FilePickerFileType>? filters = null,
        string? title = null)
    {
        var files = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true, FileTypeFilter = filters, Title = title
        });
        return files.Select(file => file.TryGetLocalPath()).OfType<string>().ToList();
    }

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
}
