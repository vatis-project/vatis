// <copyright file="VoicePacksDialogViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Voice.WavPack;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model of the window that manages vATIS's library of voice packs.
/// </summary>
public class VoicePacksDialogViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IProfileRepository _profileRepository;
    private Dictionary<string, (int Stations, int Profiles)> _usage = new();
    private WavPackInfo? _selectedPack;
    private string _packName = string.Empty;
    private string? _importSummary;

    /// <summary>
    /// Initializes a new instance of the <see cref="VoicePacksDialogViewModel"/> class.
    /// </summary>
    /// <param name="profileRepository">The profile repository, used to find which stations use a pack.</param>
    public VoicePacksDialogViewModel(IProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;

        CloseWindowCommand = ReactiveCommand.Create<ICloseable>(window => window.Close());
        ImportCommand = ReactiveCommand.CreateFromTask(HandleImport);
        RenameCommand = ReactiveCommand.Create(HandleRename,
            this.WhenAnyValue(x => x.SelectedPack, x => x.PackName,
                (pack, name) => pack != null && name.Trim().Length > 0 && name.Trim() != pack.Name));
        DeleteCommand = ReactiveCommand.CreateFromTask(HandleDelete,
            this.WhenAnyValue(x => x.SelectedPack).Select(x => x?.Id != null));
        OpenFolderCommand = ReactiveCommand.CreateFromTask(HandleOpenFolder,
            this.WhenAnyValue(x => x.SelectedPack).Select(x => x != null));

        ImportDetails.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(HasImportDetails));
    }

    /// <summary>
    /// Gets the command that closes the window.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    /// <summary>
    /// Gets the command that imports a voice pack folder into the library.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportCommand { get; }

    /// <summary>
    /// Gets the command that renames the selected pack.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RenameCommand { get; }

    /// <summary>
    /// Gets the command that deletes the selected pack.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    /// <summary>
    /// Gets the command that opens the selected pack's folder in the system file manager.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }

    /// <summary>
    /// Gets or sets the window that owns this view model's message boxes.
    /// </summary>
    public Window? Owner { get; set; }

    /// <summary>
    /// Gets the voice packs in the library.
    /// </summary>
    public ObservableCollection<WavPackInfo> Packs { get; } = [];

    /// <summary>
    /// Gets the editor for the selected pack's clips.
    /// </summary>
    public WavPackClipEditorViewModel ClipEditor { get; } = new();

    /// <summary>
    /// Gets the per-entry results of the last legacy ATIS.txt conversion.
    /// </summary>
    public ObservableCollection<string> ImportDetails { get; } = [];

    /// <summary>
    /// Gets a value indicating whether the last import produced any per-entry results.
    /// </summary>
    public bool HasImportDetails => ImportDetails.Count > 0;

    /// <summary>
    /// Gets or sets the selected pack.
    /// </summary>
    public WavPackInfo? SelectedPack
    {
        get => _selectedPack;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPack, value);
            PackName = value?.Name ?? string.Empty;
            ClipEditor.Load(value?.Folder);
            this.RaisePropertyChanged(nameof(UsageText));
        }
    }

    /// <summary>
    /// Gets or sets the name shown in the name box, which is applied by the rename command.
    /// </summary>
    public string PackName
    {
        get => _packName;
        set => this.RaiseAndSetIfChanged(ref _packName, value);
    }

    /// <summary>
    /// Gets the summary of the last import.
    /// </summary>
    public string? ImportSummary
    {
        get => _importSummary;
        private set => this.RaiseAndSetIfChanged(ref _importSummary, value);
    }

    /// <summary>
    /// Gets a description of which stations use the selected pack.
    /// </summary>
    public string? UsageText
    {
        get
        {
            if (_selectedPack?.Id == null)
            {
                return null;
            }

            return _usage.TryGetValue(_selectedPack.Id, out var u)
                ? $"Used by {u.Stations} station{(u.Stations == 1 ? string.Empty : "s")} in {u.Profiles} profile{(u.Profiles == 1 ? string.Empty : "s")}."
                : "Not used by any station.";
        }
    }

    /// <summary>
    /// Loads the library and the usage counts.
    /// </summary>
    /// <param name="selectId">The id of the pack to select, or null to keep the current selection.</param>
    /// <returns>A task that completes when loading is done.</returns>
    public async Task LoadAsync(string? selectId = null)
    {
        selectId ??= _selectedPack?.Id;
        await RefreshUsageAsync();
        Reload(selectId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CloseWindowCommand.Dispose();
        ImportCommand.Dispose();
        RenameCommand.Dispose();
        DeleteCommand.Dispose();
        OpenFolderCommand.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Reload(string? selectId)
    {
        var packs = WavPackLibrary.List();
        Packs.Clear();
        foreach (var pack in packs)
        {
            Packs.Add(pack);
        }

        SelectedPack = packs.FirstOrDefault(p => p.Id == selectId) ?? packs.FirstOrDefault();
    }

    private async Task RefreshUsageAsync()
    {
        var usage = new Dictionary<string, (int Stations, int Profiles)>();
        try
        {
            foreach (var profile in await _profileRepository.LoadAll())
            {
                foreach (var group in (profile.Stations ?? [])
                             .Where(s => s.AtisVoice is { UseWavPack: true, WavPackId: not null })
                             .GroupBy(s => s.AtisVoice.WavPackId!))
                {
                    usage.TryGetValue(group.Key, out var current);
                    usage[group.Key] = (current.Stations + group.Count(), current.Profiles + 1);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            Serilog.Log.Warning(ex, "Failed to scan profiles for voice pack usage");
        }

        _usage = usage;
        this.RaisePropertyChanged(nameof(UsageText));
    }

    private async Task HandleImport()
    {
        var folder = await FilePickerExtensions.OpenFolderPickerAsync(
            "Select a voice pack folder (manifest.json or ATIS.txt, and WAV files)");
        if (folder == null)
        {
            return;
        }

        ImportDetails.Clear();
        ImportSummary = "Importing...";
        try
        {
            var (pack, report, reused) = await Task.Run(() => WavPackLibrary.Import(folder));

            if (report != null)
            {
                var letterVariants = report.Skipped
                    .Where(x => x.Length > 2 && char.IsAsciiLetterUpper(x[0]) && x[1] == '_' &&
                                x.EndsWith("(no vATIS equivalent)", StringComparison.Ordinal))
                    .ToList();
                if (letterVariants.Count > 0)
                {
                    ImportDetails.Add(
                        $"Skipped: {letterVariants.Count} letter variants ({letterVariants[0][..2]} to {letterVariants[^1][..2]}) - no vATIS equivalent");
                }

                foreach (var skipped in report.Skipped.Except(letterVariants))
                {
                    ImportDetails.Add($"Skipped: {skipped}");
                }

                foreach (var duplicate in report.Duplicates)
                {
                    ImportDetails.Add($"Duplicate (first kept): {duplicate}");
                }
            }

            ImportSummary = reused
                ? "Already imported - selected the existing pack."
                : report != null
                    ? $"Converted ATIS.txt · {report.Skipped.Count} skipped · {report.Duplicates.Count} duplicate (details)"
                    : "Imported.";

            await LoadAsync(pack.Id);
        }
        catch (Exception ex) when (ex is WavPackException or IOException or UnauthorizedAccessException)
        {
            ImportSummary = $"Import failed: {ex.Message}";
        }
    }

    private void HandleRename()
    {
        if (_selectedPack?.Id == null)
        {
            return;
        }

        try
        {
            WavPackLibrary.Rename(_selectedPack.Id, PackName);
            Reload(_selectedPack.Id);
            ImportSummary = "Renamed.";
        }
        catch (WavPackException ex)
        {
            ImportSummary = ex.Message;
        }
    }

    private async Task HandleDelete()
    {
        if (_selectedPack?.Id == null || Owner == null)
        {
            return;
        }

        var message = $"Delete the voice pack \"{_selectedPack.Name}\" and its audio files?";
        if (_usage.TryGetValue(_selectedPack.Id, out var u))
        {
            message = $"\"{_selectedPack.Name}\" is used by {u.Stations} station(s) in {u.Profiles} profile(s). " +
                      "Delete it anyway? Those stations will have no voice pack until you choose another.";
        }

        if (await MessageBox.ShowDialog(Owner, message, "Delete Voice Pack", MessageBoxButton.YesNo,
                MessageBoxIcon.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            WavPackLibrary.Delete(_selectedPack.Id);
            ImportDetails.Clear();
            ImportSummary = "Deleted.";
            await LoadAsync(string.Empty);

            // nothing left to show a message about
            if (Packs.Count == 0)
            {
                ImportSummary = null;
            }
        }
        catch (IOException ex)
        {
            ImportSummary = $"Delete failed: {ex.Message}";
        }
    }

    private async Task HandleOpenFolder()
    {
        if (_selectedPack == null || !Directory.Exists(_selectedPack.Folder))
        {
            ImportSummary = "The voice pack folder was not found.";
            return;
        }

        if (!await FilePickerExtensions.OpenFolderInFileManagerAsync(_selectedPack.Folder))
        {
            ImportSummary = "Unable to open the voice pack folder.";
        }
    }
}
