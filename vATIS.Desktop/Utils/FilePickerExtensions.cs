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
    private static FilePickerFileType All { get; } = new("All files")
    {
        Patterns = ["*.*"],
        MimeTypes = ["*/*"]
    };

    private static FilePickerFileType Json { get; } = new("JSON files")
    {
        Patterns = ["*.json"],
        AppleUniformTypeIdentifiers = ["public.json"],
        MimeTypes = ["application/json"]
    };

    private static IStorageProvider GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow?.StorageProvider is not { } provider)
        {
            throw new NullReferenceException("Missing StorageProvider instance.");
        }

        return provider;
    }

    public static async Task<List<string>?> OpenFilePickerAsync(IReadOnlyList<FilePickerFileType>? filters = null, string? title = null)
    {
        var files = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = true,
            FileTypeFilter = filters,
            Title = title
        });
        return files.Select(file => file.TryGetLocalPath()).OfType<string>().ToList();
    }

    public static async Task<IStorageFile?> SaveFileAsync(string title, IReadOnlyList<FilePickerFileType> filters, string? initialFileName = null)
    {
        return await GetStorageProvider().SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            FileTypeChoices = filters,
            ShowOverwritePrompt = true,
            SuggestedFileName = initialFileName
        });
    }

    private static List<FilePickerFileType> GetFilePickerFileTypes(string[] filterExtTypes)
    {
        var fileTypeFilters = new List<FilePickerFileType>();

        foreach (var fileType in filterExtTypes)
        {
            switch (fileType)
            {
                case "*":
                    {
                        fileTypeFilters.Add(All);
                        break;
                    }
                case "json":
                    {
                        fileTypeFilters.Add(Json);
                        break;
                    }
            }
        }

        return fileTypeFilters;
    }
}
