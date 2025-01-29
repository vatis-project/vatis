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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using ReactiveUI;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Io;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Controls;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Models;
using Vatsim.Vatis.Weather;

namespace Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

/// <summary>
/// Provides the ViewModel for managing ATIS presets and configurations.
/// </summary>
public class PresetsViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IDownloader _downloader;
    private readonly HashSet<string> _initializedProperties = [];
    private readonly IMetarRepository _metarRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly CompositeDisposable _disposables = [];
    private bool _hasUnsavedChanges;
    private ObservableCollection<AtisPreset>? _presets;
    private List<ICompletionData> _contractionCompletionData = new();
    private AtisStation? _selectedStation;
    private AtisPreset? _selectedPreset;
    private bool _useExternalAtisGenerator;
    private string? _externalGeneratorUrl;
    private string? _externalGeneratorArrivalRunways;
    private string? _externalGeneratorDepartureRunways;
    private string? _externalGeneratorApproaches;
    private string? _externalGeneratorRemarks;
    private string? _externalGeneratorSandboxResponse;
    private TextDocument? _atisTemplateTextDocument = new();
    private string? _sandboxMetar;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetsViewModel"/> class.
    /// </summary>
    /// <param name="windowFactory">The factory for creating window instances.</param>
    /// <param name="downloader">The service responsible for downloading resources.</param>
    /// <param name="metarRepository">The repository interface for accessing METAR data.</param>
    /// <param name="profileRepository">The repository interface for managing profiles.</param>
    /// <param name="sessionManager">The session manager for handling session-related activities.</param>
    public PresetsViewModel(
        IWindowFactory windowFactory,
        IDownloader downloader,
        IMetarRepository metarRepository,
        IProfileRepository profileRepository,
        ISessionManager sessionManager)
    {
        _windowFactory = windowFactory;
        _downloader = downloader;
        _metarRepository = metarRepository;
        _profileRepository = profileRepository;
        _sessionManager = sessionManager;

        AtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleUpdateProperties);
        SelectedPresetChanged = ReactiveCommand.Create<AtisPreset>(HandleSelectedPresetChanged);
        NewPresetCommand = ReactiveCommand.CreateFromTask(HandleNewPreset);
        RenamePresetCommand = ReactiveCommand.CreateFromTask(HandleRenamePreset);
        CopyPresetCommand = ReactiveCommand.CreateFromTask(HandleCopyPreset);
        DeletePresetCommand = ReactiveCommand.CreateFromTask(HandleDeletePreset);
        OpenSortPresetsDialogCommand = ReactiveCommand.CreateFromTask(HandleOpenSortPresetsDialog);
        TestExternalGeneratorCommand = ReactiveCommand.CreateFromTask(HandleTestExternalGenerator);
        TemplateVariableClicked = ReactiveCommand.Create<string>(HandleTemplateVariableClicked);
        FetchSandboxMetarCommand = ReactiveCommand.CreateFromTask(HandleFetchSandboxMetar);

        _disposables.Add(AtisStationChanged);
        _disposables.Add(SelectedPresetChanged);
        _disposables.Add(NewPresetCommand);
        _disposables.Add(RenamePresetCommand);
        _disposables.Add(CopyPresetCommand);
        _disposables.Add(DeletePresetCommand);
        _disposables.Add(OpenSortPresetsDialogCommand);
        _disposables.Add(TestExternalGeneratorCommand);
        _disposables.Add(TemplateVariableClicked);
        _disposables.Add(FetchSandboxMetarCommand);
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
    public ReactiveCommand<Unit, Unit> TestExternalGeneratorCommand { get; }

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
            if (!_initializedProperties.Add(nameof(UseExternalAtisGenerator)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator URL.
    /// </summary>
    public string? ExternalGeneratorUrl
    {
        get => _externalGeneratorUrl;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorUrl, value);
            if (!_initializedProperties.Add(nameof(ExternalGeneratorUrl)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator arrival runways.
    /// </summary>
    public string? ExternalGeneratorArrivalRunways
    {
        get => _externalGeneratorArrivalRunways;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorArrivalRunways, value);
            if (!_initializedProperties.Add(nameof(ExternalGeneratorArrivalRunways)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator departure runways.
    /// </summary>
    public string? ExternalGeneratorDepartureRunways
    {
        get => _externalGeneratorDepartureRunways;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorDepartureRunways, value);
            if (!_initializedProperties.Add(nameof(ExternalGeneratorDepartureRunways)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator approaches.
    /// </summary>
    public string? ExternalGeneratorApproaches
    {
        get => _externalGeneratorApproaches;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorApproaches, value);
            if (!_initializedProperties.Add(nameof(ExternalGeneratorApproaches)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator remarks.
    /// </summary>
    public string? ExternalGeneratorRemarks
    {
        get => _externalGeneratorRemarks;
        set
        {
            this.RaiseAndSetIfChanged(ref _externalGeneratorRemarks, value);
            if (!_initializedProperties.Add(nameof(ExternalGeneratorRemarks)))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator sandbox response.
    /// </summary>
    public string? ExternalGeneratorSandboxResponse
    {
        get => _externalGeneratorSandboxResponse;
        set => this.RaiseAndSetIfChanged(ref _externalGeneratorSandboxResponse, value);
    }

    /// <summary>
    /// Gets or sets the ATIS template text.
    /// </summary>
    public string AtisTemplateText
    {
        get => _atisTemplateTextDocument?.Text ?? string.Empty;
        set => AtisTemplateTextDocument = new TextDocument(value);
    }

    /// <summary>
    /// Gets or sets the ATIS template text document.
    /// </summary>
    public TextDocument? AtisTemplateTextDocument
    {
        get => _atisTemplateTextDocument;
        set => this.RaiseAndSetIfChanged(ref _atisTemplateTextDocument, value);
    }

    /// <summary>
    /// Gets or sets the sandbox METAR.
    /// </summary>
    public string? SandboxMetar
    {
        get => _sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref _sandboxMetar, value);
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
            return true;
        }

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

            if (SelectedPreset.ExternalGenerator.Url != ExternalGeneratorUrl)
            {
                SelectedPreset.ExternalGenerator.Url = ExternalGeneratorUrl;
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

        HasUnsavedChanges = false;

        return true;
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

        var metar = await _metarRepository.GetMetar(
            SelectedStation.Identifier,
            false,
            false);
        SandboxMetar = metar?.RawMetar;
    }

    private async Task HandleTestExternalGenerator()
    {
        if (!string.IsNullOrEmpty(ExternalGeneratorUrl))
        {
            var url = ExternalGeneratorUrl;
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
            }

            url = url.Replace("$metar", HttpUtility.UrlEncode(SandboxMetar));
            url = url.Replace("$arrrwy", ExternalGeneratorArrivalRunways);
            url = url.Replace("$deprwy", ExternalGeneratorDepartureRunways);
            url = url.Replace("$app", ExternalGeneratorApproaches);
            url = url.Replace("$remarks", ExternalGeneratorRemarks);
            url = url.Replace("$atiscode", StringExtensions.RandomLetter());

            var response = await _downloader.DownloadStringAsync(url);

            response = Regex.Replace(response, @"\[(.*?)\]", " $1 ");
            response = Regex.Replace(response, @"\s+", " ");

            ExternalGeneratorSandboxResponse = response.Trim();
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

        SelectedPreset = preset;

        UseExternalAtisGenerator = false;
        ExternalGeneratorUrl = null;
        ExternalGeneratorArrivalRunways = null;
        ExternalGeneratorDepartureRunways = null;
        ExternalGeneratorApproaches = null;
        ExternalGeneratorRemarks = null;
        ExternalGeneratorSandboxResponse = null;
        AtisTemplateText = SelectedPreset?.Template ?? string.Empty;

        if (SelectedPreset?.ExternalGenerator != null)
        {
            UseExternalAtisGenerator = SelectedPreset.ExternalGenerator.Enabled;
            ExternalGeneratorUrl = SelectedPreset.ExternalGenerator.Url;
            ExternalGeneratorArrivalRunways = SelectedPreset.ExternalGenerator.Arrival;
            ExternalGeneratorDepartureRunways = SelectedPreset.ExternalGenerator.Departure;
            ExternalGeneratorApproaches = SelectedPreset.ExternalGenerator.Approaches;
            ExternalGeneratorRemarks = SelectedPreset.ExternalGenerator.Remarks;
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
        ExternalGeneratorUrl = null;
        ExternalGeneratorArrivalRunways = null;
        ExternalGeneratorDepartureRunways = null;
        ExternalGeneratorApproaches = null;
        ExternalGeneratorRemarks = null;
        ExternalGeneratorSandboxResponse = null;
        AtisTemplateText = string.Empty;

        ContractionCompletionData = [];
        foreach (var contraction in station.Contractions)
        {
            if (!string.IsNullOrEmpty(contraction.VariableName) && !string.IsNullOrEmpty(contraction.Voice))
            {
                ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
            }
        }

        HasUnsavedChanges = false;
    }
}
