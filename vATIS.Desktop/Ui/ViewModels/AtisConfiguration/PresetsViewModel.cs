// <copyright file="PresetsViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Editing;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Controls;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;
using Vatsim.Vatis.Voice.Audio;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Provides the ViewModel for managing ATIS presets and configurations.
/// </summary>
public class PresetsViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IMetarRepository _metarRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly IAtisBuilder _atisBuilder;
    private readonly CompositeDisposable _disposables = [];
    private readonly Dictionary<string, (object? OriginalValue, object? CurrentValue)> _fieldHistory;
    private bool _hasUnsavedChanges;
    private ObservableCollection<AtisPreset>? _presets;
    private List<ICompletionData> _contractionCompletionData = new();
    private AtisStation? _selectedStation;
    private AtisPreset? _selectedPreset;
    private bool _useExternalAtisGenerator;
    private string _externalGeneratorTextUrl = string.Empty;
    private string _externalGeneratorVoiceUrl = string.Empty;
    private string _externalGeneratorArrivalRunways = string.Empty;
    private string _externalGeneratorDepartureRunways = string.Empty;
    private string _externalGeneratorApproaches = string.Empty;
    private string _externalGeneratorRemarks = string.Empty;
    private string? _externalGeneratorSandboxResponseText;
    private string? _externalGeneratorSandboxResponseVoice;
    private bool _isSandboxPlaybackActive;
    private AtisBuilderVoiceAtisResponse? _atisBuilderVoiceResponse;
    private CancellationTokenSource _externalGeneratorCancellationTokenSource = new();
    private string _atisTemplateText = string.Empty;
    private string? _sandboxMetar;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetsViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">The factory for creating window instances.</param>
    /// <param name="metarRepository">The repository interface for accessing METAR data.</param>
    /// <param name="profileRepository">The repository interface for managing profiles.</param>
    /// <param name="sessionManager">The session manager for handling session-related activities.</param>
    /// <param name="atisBuilder">The ATIS builder service.</param>
    public PresetsViewModel(
        IWindowFactory windowFactory,
        IMetarRepository metarRepository,
        IProfileRepository profileRepository,
        ISessionManager sessionManager,
        IAtisBuilder atisBuilder)
    {
        _windowFactory = windowFactory;
        _metarRepository = metarRepository;
        _profileRepository = profileRepository;
        _sessionManager = sessionManager;
        _atisBuilder = atisBuilder;
        _fieldHistory = [];

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleUpdateProperties);
        SelectedPresetChanged = ReactiveCommand.Create<AtisPreset>(HandleSelectedPresetChanged);
        NewPresetCommand = ReactiveCommand.CreateFromTask(HandleNewPreset);
        RenamePresetCommand = ReactiveCommand.CreateFromTask(HandleRenamePreset);
        CopyPresetCommand = ReactiveCommand.CreateFromTask(HandleCopyPreset);
        DeletePresetCommand = ReactiveCommand.CreateFromTask(HandleDeletePreset);
        OpenSortPresetsDialogCommand = ReactiveCommand.CreateFromTask(HandleOpenSortPresetsDialog);
        TemplateVariableClicked = ReactiveCommand.Create<string>(HandleTemplateVariableClicked);

        FetchSandboxMetarCommand = ReactiveCommand.CreateFromTask(HandleFetchSandboxMetar);
        GenerateSandboxAtisCommand = ReactiveCommand.CreateFromTask(HandleGenerateSandboxAtis);
        PlaySandboxVoiceAtisCommand = ReactiveCommand.Create(HandlePlaySandboxVoiceAtis, this.WhenAnyValue(
            x => x.AtisBuilderVoiceResponse, resp => resp?.AudioBytes != null));

        _disposables.Add(AtisStationChanged);
        _disposables.Add(SelectedPresetChanged);
        _disposables.Add(NewPresetCommand);
        _disposables.Add(RenamePresetCommand);
        _disposables.Add(CopyPresetCommand);
        _disposables.Add(DeletePresetCommand);
        _disposables.Add(OpenSortPresetsDialogCommand);
        _disposables.Add(GenerateSandboxAtisCommand);
        _disposables.Add(TemplateVariableClicked);
        _disposables.Add(FetchSandboxMetarCommand);

        _disposables.Add(EventBus.Instance.Subscribe<ContractionsUpdated>(evt =>
        {
            if (evt.StationId == SelectedStation?.Id)
            {
                PopulateContractions();
            }
        }));
    }

    /// <summary>
    /// Gets or sets the owner of a dialog associated with this instance.
    /// </summary>
    public IDialogOwner? DialogOwner { get; set; }

    /// <summary>
    /// Gets a command that handles changes to the ATIS station.
    /// </summary>
    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    /// <summary>
    /// Gets a command that handles changes to the selected preset.
    /// </summary>
    public ReactiveCommand<AtisPreset, Unit> SelectedPresetChanged { get; }

    /// <summary>
    /// Gets a command that creates a new ATIS preset.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewPresetCommand { get; }

    /// <summary>
    /// Gets a command that renames the selected preset.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RenamePresetCommand { get; }

    /// <summary>
    /// Gets a command that copies the selected preset.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CopyPresetCommand { get; }

    /// <summary>
    /// Gets a command that deletes the selected preset.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeletePresetCommand { get; }

    /// <summary>
    /// Gets a command that opens the dialog for sorting presets.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenSortPresetsDialogCommand { get; }

    /// <summary>
    /// Gets a command that tests the external ATIS generator.
    /// </summary>
    public ReactiveCommand<Unit, Unit> GenerateSandboxAtisCommand { get; }

    /// <summary>
    /// Gets a command that plays the voice ATIS created from the external ATIS generator.
    /// </summary>
    public ReactiveCommand<Unit, Unit> PlaySandboxVoiceAtisCommand { get; }

    /// <summary>
    /// Gets a command that handles template variable clicks.
    /// </summary>
    public ReactiveCommand<string, Unit> TemplateVariableClicked { get; }

    /// <summary>
    /// Gets a command that fetches sandbox METAR data.
    /// </summary>
    public ReactiveCommand<Unit, Unit> FetchSandboxMetarCommand { get; }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets the collection of ATIS presets.
    /// </summary>
    public ObservableCollection<AtisPreset>? Presets
    {
        get => _presets;
        set => this.RaiseAndSetIfChanged(ref _presets, value);
    }

    /// <summary>
    /// Gets or sets the contraction completion data.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS preset.
    /// </summary>
    public AtisPreset? SelectedPreset
    {
        get => _selectedPreset;
        set => this.RaiseAndSetIfChanged(ref _selectedPreset, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use an external ATIS generator.
    /// </summary>
    public bool UseExternalAtisGenerator
    {
        get => _useExternalAtisGenerator;
        set
        {
            this.RaiseAndSetIfChanged(ref _useExternalAtisGenerator, value);
            TrackChanges(nameof(UseExternalAtisGenerator), value);
        }
    }

    /// <summary>
    /// Gets or sets the URL for generating the text ATIS string.
    /// </summary>
    public string ExternalGeneratorTextUrl
    {
        get => _externalGeneratorTextUrl;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorTextUrl, value);
            TrackChanges(nameof(ExternalGeneratorTextUrl), value);
        }
    }

    /// <summary>
    /// Gets or sets the URL for generating the voice ATIS string.
    /// </summary>
    public string ExternalGeneratorVoiceUrl
    {
        get => _externalGeneratorVoiceUrl;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorVoiceUrl, value);
            TrackChanges(nameof(ExternalGeneratorVoiceUrl), value);
        }
    }

    /// <summary>
    /// Gets or sets the external generator arrival runways.
    /// </summary>
    public string ExternalGeneratorArrivalRunways
    {
        get => _externalGeneratorArrivalRunways;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorArrivalRunways, value);
            TrackChanges(nameof(ExternalGeneratorArrivalRunways), value);
        }
    }

    /// <summary>
    /// Gets or sets the external generator departure runways.
    /// </summary>
    public string ExternalGeneratorDepartureRunways
    {
        get => _externalGeneratorDepartureRunways;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorDepartureRunways, value);
            TrackChanges(nameof(ExternalGeneratorDepartureRunways), value);
        }
    }

    /// <summary>
    /// Gets or sets the external generator approaches.
    /// </summary>
    public string ExternalGeneratorApproaches
    {
        get => _externalGeneratorApproaches;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorApproaches, value);
            TrackChanges(nameof(ExternalGeneratorApproaches), value);
        }
    }

    /// <summary>
    /// Gets or sets the external generator remarks.
    /// </summary>
    public string ExternalGeneratorRemarks
    {
        get => _externalGeneratorRemarks;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorRemarks, value);
            TrackChanges(nameof(ExternalGeneratorRemarks), value);
        }
    }

    /// <summary>
    /// Gets or sets the external generator sandbox text response.
    /// </summary>
    public string? ExternalGeneratorSandboxResponseText
    {
        get => _externalGeneratorSandboxResponseText;
        set => this.RaiseAndSetIfChanged(ref _externalGeneratorSandboxResponseText, value);
    }

    /// <summary>
    /// Gets or sets the external generator sandbox voice response.
    /// </summary>
    public string? ExternalGeneratorSandboxResponseVoice
    {
        get => _externalGeneratorSandboxResponseVoice;
        set => this.RaiseAndSetIfChanged(ref _externalGeneratorSandboxResponseVoice, value);
    }

    /// <summary>
    /// Gets or sets the ATIS template text document.
    /// </summary>
    public string AtisTemplateText
    {
        get => _atisTemplateText;
        set
        {
            this.RaiseAndSetIfChanged(ref _atisTemplateText, value);
            TrackChanges(nameof(AtisTemplateText), value);
        }
    }

    /// <summary>
    /// Gets or sets the sandbox METAR.
    /// </summary>
    public string? SandboxMetar
    {
        get => _sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref _sandboxMetar, value);
    }

    /// <summary>
    /// Gets or sets the ATIS builder response.
    /// </summary>
    public AtisBuilderVoiceAtisResponse? AtisBuilderVoiceResponse
    {
        get => _atisBuilderVoiceResponse;
        set => this.RaiseAndSetIfChanged(ref _atisBuilderVoiceResponse, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether sandbox playback is active.
    /// </summary>
    public bool IsSandboxPlaybackActive
    {
        get => _isSandboxPlaybackActive;
        set => this.RaiseAndSetIfChanged(ref _isSandboxPlaybackActive, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Applies the configuration changes for the currently selected preset and updates the session profile if applicable.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the configuration was successfully applied. Returns true if successful; otherwise, false.
    /// </returns>
    public bool ApplyConfig()
    {
        if (SelectedPreset == null)
        {
            return false;
        }

        IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();

        if (SelectedPreset.Template != AtisTemplateText)
        {
            SelectedPreset.Template = AtisTemplateText;
        }

        if (SelectedPreset is { ExternalGenerator: not null })
        {
            if (SelectedPreset.ExternalGenerator.Enabled != UseExternalAtisGenerator)
            {
                SelectedPreset.ExternalGenerator.Enabled = UseExternalAtisGenerator;
            }

            if (SelectedPreset.ExternalGenerator.TextUrl != ExternalGeneratorTextUrl)
            {
                SelectedPreset.ExternalGenerator.TextUrl = ExternalGeneratorTextUrl;
            }

            if (SelectedPreset.ExternalGenerator.VoiceUrl != ExternalGeneratorVoiceUrl)
            {
                SelectedPreset.ExternalGenerator.VoiceUrl = ExternalGeneratorVoiceUrl;
            }

            if (SelectedPreset.ExternalGenerator.Arrival != ExternalGeneratorArrivalRunways)
            {
                SelectedPreset.ExternalGenerator.Arrival = ExternalGeneratorArrivalRunways;
            }

            if (SelectedPreset.ExternalGenerator.Departure != ExternalGeneratorDepartureRunways)
            {
                SelectedPreset.ExternalGenerator.Departure = ExternalGeneratorDepartureRunways;
            }

            if (SelectedPreset.ExternalGenerator.Approaches != ExternalGeneratorApproaches)
            {
                SelectedPreset.ExternalGenerator.Approaches = ExternalGeneratorApproaches;
            }

            if (SelectedPreset.ExternalGenerator.Remarks != ExternalGeneratorRemarks)
            {
                SelectedPreset.ExternalGenerator.Remarks = ExternalGeneratorRemarks;
            }
        }

        if (HasErrors)
        {
            return false;
        }

        if (_sessionManager.CurrentProfile != null)
        {
            _profileRepository.Save(_sessionManager.CurrentProfile);
        }

        // Check if there are unsaved changes before saving
        var anyChanges = _fieldHistory.Any(entry => !AreValuesEqual(entry.Value.OriginalValue, entry.Value.CurrentValue));

        // If there are unsaved changes, mark as applied and reset HasUnsavedChanges
        if (anyChanges)
        {
            // Apply changes and reset HasUnsavedChanges
            foreach (var key in _fieldHistory.Keys.ToList())
            {
                var (_, currentValue) = _fieldHistory[key];

                // After applying changes, set the original value to the current value (the saved value)
                _fieldHistory[key] = (currentValue, currentValue);
            }

            HasUnsavedChanges = false;
            return true;
        }

        return false;
    }

    private static void HandleTemplateVariableClicked(string? variable)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var topLevel = TopLevel.GetTopLevel(lifetime.MainWindow);
            var focusedElement = topLevel?.FocusManager?.GetFocusedElement();

            variable = variable?.Replace("__", "_") ?? string.Empty;

            if (focusedElement is TemplateVariableTextBox focusedTextBox)
            {
                var caretIndex = focusedTextBox.CaretIndex;
                focusedTextBox.Text = focusedTextBox.Text?.Insert(focusedTextBox.CaretIndex, variable);
                focusedTextBox.CaretIndex = variable.Length + caretIndex;
            }

            if (focusedElement is TextArea focusedTextEditor)
            {
                var caretIndex = focusedTextEditor.Caret.Offset;
                focusedTextEditor.Document.Text =
                    focusedTextEditor.Document.Text.Insert(focusedTextEditor.Caret.Offset, variable);
                focusedTextEditor.Caret.Offset = variable.Length + caretIndex;
            }
        }
    }

    private async Task HandleFetchSandboxMetar()
    {
        if (SelectedStation == null || string.IsNullOrEmpty(SelectedStation.Identifier))
        {
            return;
        }

        var metar = await _metarRepository.GetMetar(SelectedStation.Identifier, false, false);
        SandboxMetar = metar?.RawMetar;
    }

    private async Task HandleGenerateSandboxAtis()
    {
        try
        {
            if (SelectedStation == null || SelectedPreset == null)
                return;

            NativeAudio.StopBufferPlayback();
            AtisBuilderVoiceResponse = null;

            await _externalGeneratorCancellationTokenSource.CancelAsync();
            _externalGeneratorCancellationTokenSource = new CancellationTokenSource();
            var localToken = _externalGeneratorCancellationTokenSource;

            ExternalGeneratorSandboxResponseText = "Loading...";
            ExternalGeneratorSandboxResponseVoice = "Loading...";

            var randomLetter = StringExtensions.RandomLetter();

            if (!string.IsNullOrEmpty(ExternalGeneratorTextUrl))
            {
                ExternalGeneratorSandboxResponseText =
                    await _atisBuilder.GetExternalTextAtis(SelectedStation, SelectedPreset, randomLetter, SandboxMetar);
            }

            if (!string.IsNullOrEmpty(ExternalGeneratorVoiceUrl))
            {
                var voiceAtis = await _atisBuilder.GetExternalVoiceAtis(SelectedStation, SelectedPreset, randomLetter,
                    SandboxMetar, localToken.Token);
                ExternalGeneratorSandboxResponseVoice = voiceAtis?.SpokenText;
                AtisBuilderVoiceResponse = voiceAtis;
            }
        }
        catch (Exception ex)
        {
            ExternalGeneratorSandboxResponseText = "Error fetching text ATIS. See log for details.";
            ExternalGeneratorSandboxResponseVoice = "Error fetching voice ATIS. See log for details.";
            Log.Error(ex, "Failed to generate sandbox ATIS.");
        }
    }

    private void HandlePlaySandboxVoiceAtis()
    {
        if (AtisBuilderVoiceResponse?.AudioBytes == null)
            return;

        if (!IsSandboxPlaybackActive)
        {
            IsSandboxPlaybackActive = NativeAudio.StartBufferPlayback(AtisBuilderVoiceResponse.AudioBytes,
                AtisBuilderVoiceResponse.AudioBytes.Length);
        }
        else
        {
            IsSandboxPlaybackActive = false;
            NativeAudio.StopBufferPlayback();
        }
    }

    private async Task HandleOpenSortPresetsDialog()
    {
        if (DialogOwner == null)
        {
            return;
        }

        if (SelectedStation == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var dialog = _windowFactory.CreateSortPresetsDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is SortPresetsDialogViewModel context)
            {
                context.Presets = new ObservableCollection<AtisPreset>(SelectedStation.Presets);

                context.Presets.CollectionChanged += (_, _) =>
                {
                    var idx = 0;
                    SelectedStation.Presets.Clear();
                    foreach (var item in context.Presets)
                    {
                        item.Ordinal = ++idx;
                        SelectedStation.Presets.Add(item);
                    }

                    if (_sessionManager.CurrentProfile != null)
                    {
                        _profileRepository.Save(_sessionManager.CurrentProfile);
                    }

                    RefreshPresetList();
                };

                await dialog.ShowDialog((Window)DialogOwner);
            }
        }
    }

    private async Task HandleDeletePreset()
    {
        if (DialogOwner == null)
        {
            return;
        }

        if (SelectedStation != null && SelectedPreset != null)
        {
            if (await MessageBox.ShowDialog(
                    (Window)DialogOwner,
                    "Are you sure you want to delete the selected Preset? This action cannot be undone.",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information) == MessageBoxResult.Yes)
            {
                SelectedStation.Presets.Remove(SelectedPreset);
                if (_sessionManager.CurrentProfile != null)
                {
                    _profileRepository.Save(_sessionManager.CurrentProfile);
                }

                RefreshPresetList();
            }
        }
    }

    private async Task HandleCopyPreset()
    {
        if (SelectedStation == null || SelectedPreset == null)
        {
            return;
        }

        if (DialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousValue = SelectedPreset.Name;

            var dialog = _windowFactory.CreateUserInputDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is UserInputDialogViewModel context)
            {
                context.Prompt = "Preset Name:";
                context.Title = "Copy Preset";
                context.UserValue = previousValue;
                context.ForceUppercase = true;
                context.DialogResultChanged += (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        context.ClearError();

                        if (string.IsNullOrWhiteSpace(context.UserValue))
                        {
                            context.SetError("Preset name is required.");
                        }
                        else
                        {
                            if (SelectedStation.Presets.Any(x => x.Name == context.UserValue))
                            {
                                context.SetError(
                                    "Another preset already exists with that name. " +
                                    "Please choose a new name.");
                                return;
                            }

                            var copy = SelectedPreset.Clone();
                            copy.Ordinal = SelectedStation.Presets.Select(x => x.Ordinal).Max() + 1;
                            copy.Name = context.UserValue.Trim();
                            SelectedStation.Presets.Add(copy);
                            if (_sessionManager.CurrentProfile != null)
                            {
                                _profileRepository.Save(_sessionManager.CurrentProfile);
                            }

                            RefreshPresetList();
                            context.ClearError();
                        }
                    }
                };
            }

            await dialog.ShowDialog((Window)DialogOwner);
        }
    }

    private async Task HandleRenamePreset()
    {
        if (SelectedStation == null || SelectedPreset == null)
        {
            return;
        }

        if (DialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousValue = SelectedPreset.Name;

            var dialog = _windowFactory.CreateUserInputDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is UserInputDialogViewModel context)
            {
                context.Prompt = "Preset Name:";
                context.Title = "Rename Preset";
                context.UserValue = previousValue;
                context.ForceUppercase = true;
                context.DialogResultChanged += (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        context.ClearError();

                        if (string.IsNullOrWhiteSpace(context.UserValue))
                        {
                            context.SetError("Preset name is required.");
                        }
                        else
                        {
                            if (SelectedStation.Presets.Any(
                                    x => x.Name == context.UserValue && x != SelectedPreset))
                            {
                                context.SetError(
                                    "Another preset already exists with that name. Please choose a new name.");
                                return;
                            }

                            SelectedPreset.Name = context.UserValue.Trim();
                            if (_sessionManager.CurrentProfile != null)
                            {
                                _profileRepository.Save(_sessionManager.CurrentProfile);
                            }

                            RefreshPresetList();

                            context.ClearError();
                        }
                    }
                };
            }

            await dialog.ShowDialog((Window)DialogOwner);
        }
    }

    private void HandleSelectedPresetChanged(AtisPreset? preset)
    {
        if (preset == null)
        {
            return;
        }

        _fieldHistory.Clear();

        SelectedPreset = preset;
        ExternalGeneratorSandboxResponseText = null;
        AtisTemplateText = SelectedPreset?.Template ?? string.Empty;

        if (SelectedPreset?.ExternalGenerator != null)
        {
            UseExternalAtisGenerator = SelectedPreset.ExternalGenerator.Enabled;
            ExternalGeneratorVoiceUrl = SelectedPreset.ExternalGenerator.VoiceUrl ?? "";
            ExternalGeneratorTextUrl = SelectedPreset.ExternalGenerator.TextUrl ?? "";
            ExternalGeneratorArrivalRunways = SelectedPreset.ExternalGenerator.Arrival ?? "";
            ExternalGeneratorDepartureRunways = SelectedPreset.ExternalGenerator.Departure ?? "";
            ExternalGeneratorApproaches = SelectedPreset.ExternalGenerator.Approaches ?? "";
            ExternalGeneratorRemarks = SelectedPreset.ExternalGenerator.Remarks ?? "";
        }

        HasUnsavedChanges = false;
    }

    private async Task HandleNewPreset()
    {
        if (SelectedStation == null)
        {
            return;
        }

        if (DialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousValue = string.Empty;

            var dialog = _windowFactory.CreateUserInputDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is UserInputDialogViewModel context)
            {
                context.Prompt = "Preset Name:";
                context.Title = "New Preset";
                context.UserValue = previousValue;
                context.ForceUppercase = true;
                context.DialogResultChanged += (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        context.ClearError();

                        if (string.IsNullOrWhiteSpace(context.UserValue))
                        {
                            context.SetError("Preset name is required.");
                        }
                        else
                        {
                            if (SelectedStation.Presets.Any(x => x.Name == context.UserValue))
                            {
                                context.SetError(
                                    "Another preset already exists with that name. Please choose a new name.");
                                return;
                            }

                            var preset = new AtisPreset
                            {
                                Ordinal = SelectedStation.Presets.Any()
                                    ? SelectedStation.Presets.Select(x => x.Ordinal).Max() + 1
                                    : 0,
                                Name = context.UserValue.Trim(),
                                Template = "[FACILITY] ATIS INFO [ATIS_CODE] [TIME]. [WX]. [ARPT_COND] [NOTAMS]",
                            };
                            SelectedStation.Presets.Add(preset);
                            if (_sessionManager.CurrentProfile != null)
                            {
                                _profileRepository.Save(_sessionManager.CurrentProfile);
                            }

                            RefreshPresetList();

                            context.ClearError();
                        }
                    }
                };
            }

            await dialog.ShowDialog((Window)DialogOwner);
        }
    }

    private void RefreshPresetList()
    {
        if (SelectedStation != null)
        {
            SelectedPreset = null;
            UseExternalAtisGenerator = false;
            AtisTemplateText = string.Empty;
            Presets = new ObservableCollection<AtisPreset>(SelectedStation.Presets);
            EventBus.Instance.Publish(new StationPresetsChanged(SelectedStation.Id));
            _fieldHistory.Clear();
            HasUnsavedChanges = false;
        }
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        SelectedStation = station;
        Presets = new ObservableCollection<AtisPreset>(station.Presets);
        UseExternalAtisGenerator = false;
        SandboxMetar = null;
        AtisTemplateText = "";
        ExternalGeneratorTextUrl = "";
        ExternalGeneratorArrivalRunways = "";
        ExternalGeneratorDepartureRunways = "";
        ExternalGeneratorApproaches = "";
        ExternalGeneratorRemarks = "";
        ExternalGeneratorSandboxResponseText = null;
        ExternalGeneratorSandboxResponseVoice = null;

        IsSandboxPlaybackActive = false;
        NativeAudio.StopBufferPlayback();

        PopulateContractions();

        _fieldHistory.Clear();
        HasUnsavedChanges = false;
    }

    private void PopulateContractions()
    {
        if (SelectedStation == null)
        {
            return;
        }

        ContractionCompletionData = [];
        foreach (var contraction in SelectedStation.Contractions.ToList())
        {
            if (contraction is { VariableName: not null, Voice: not null })
                ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
        }
    }

    private void TrackChanges(string propertyName, object? currentValue)
    {
        if (_fieldHistory.TryGetValue(propertyName, out var value))
        {
            var (originalValue, _) = value;

            // If the property has been set before, update the current value
            _fieldHistory[propertyName] = (originalValue, currentValue);

            // Check for unsaved changes after the update
            CheckForUnsavedChanges();
        }
        else
        {
            // If this is the first time setting the value, initialize the original value
            _fieldHistory[propertyName] = (currentValue, currentValue);

            // Check for unsaved changes after the update
            CheckForUnsavedChanges();
        }
    }

    private bool AreValuesEqual(object? originalValue, object? currentValue)
    {
        // If both are null, consider them equal
        if (originalValue == null && currentValue == null)
            return true;

        // If one is null and the other is not, they are not equal
        if (originalValue == null || currentValue == null)
            return false;

        // Handle comparison for nullable types like int?, string?, bool? etc.
        if (originalValue is string originalStr && currentValue is string currentStr)
        {
            return originalStr == currentStr;
        }

        // Compare nullable int (int?)
        if (originalValue is int originalInt && currentValue is int currentInt)
        {
            return originalInt == currentInt;
        }

        // Compare nullable bool (bool?)
        if (originalValue is bool originalBool && currentValue is bool currentBool)
        {
            return originalBool == currentBool;
        }

        if (originalValue is IEnumerable<object> originalEnumerable &&
            currentValue is IEnumerable<object> currentEnumerable)
        {
            return AreListsEqual(originalEnumerable, currentEnumerable);
        }

        // For non-nullable types (or if types are not specifically handled above), use Equals
        return originalValue.Equals(currentValue);
    }

    private bool AreListsEqual(IEnumerable<object> list1, IEnumerable<object> list2)
    {
        return list1.SequenceEqual(list2);
    }

    private void CheckForUnsavedChanges()
    {
        var anyChanges = _fieldHistory.Any(entry => !AreValuesEqual(entry.Value.OriginalValue, entry.Value.CurrentValue));
        HasUnsavedChanges = anyChanges;
    }
}
