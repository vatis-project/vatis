// <copyright file="PresetsViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
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
public class PresetsViewModel : ReactiveViewModelBase
{
    private readonly IDownloader downloader;
    private readonly HashSet<string> initializedProperties = [];
    private readonly IMetarRepository metarRepository;
    private readonly IProfileRepository profileRepository;
    private readonly ISessionManager sessionManager;
    private readonly IWindowFactory windowFactory;
    private bool hasUnsavedChanges;
    private ObservableCollection<AtisPreset>? presets;
    private List<ICompletionData> contractionCompletionData = new();
    private AtisStation? selectedStation;
    private AtisPreset? selectedPreset;
    private bool useExternalAtisGenerator;
    private string? externalGeneratorUrl;
    private string? externalGeneratorArrivalRunways;
    private string? externalGeneratorDepartureRunways;
    private string? externalGeneratorApproaches;
    private string? externalGeneratorRemarks;
    private string? externalGeneratorSandboxResponse;
    private TextDocument? atisTemplateTextDocument = new();
    private string? sandboxMetar;

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
        this.windowFactory = windowFactory;
        this.downloader = downloader;
        this.metarRepository = metarRepository;
        this.profileRepository = profileRepository;
        this.sessionManager = sessionManager;

        this.AtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleUpdateProperties);
        this.SelectedPresetChanged = ReactiveCommand.Create<AtisPreset>(this.HandleSelectedPresetChanged);
        this.NewPresetCommand = ReactiveCommand.CreateFromTask(this.HandleNewPreset);
        this.RenamePresetCommand = ReactiveCommand.CreateFromTask(this.HandleRenamePreset);
        this.CopyPresetCommand = ReactiveCommand.CreateFromTask(this.HandleCopyPreset);
        this.DeletePresetCommand = ReactiveCommand.CreateFromTask(this.HandleDeletePreset);
        this.OpenSortPresetsDialogCommand = ReactiveCommand.CreateFromTask(this.HandleOpenSortPresetsDialog);
        this.TestExternalGeneratorCommand = ReactiveCommand.CreateFromTask(this.HandleTestExternalGenerator);
        this.TemplateVariableClicked = ReactiveCommand.Create<string>(HandleTemplateVariableClicked);
        this.FetchSandboxMetarCommand = ReactiveCommand.CreateFromTask(this.HandleFetchSandboxMetar);
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
        get => this.hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref this.hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets the collection of ATIS presets.
    /// </summary>
    public ObservableCollection<AtisPreset>? Presets
    {
        get => this.presets;
        set => this.RaiseAndSetIfChanged(ref this.presets, value);
    }

    /// <summary>
    /// Gets or sets the contraction completion data.
    /// </summary>
    public List<ICompletionData> ContractionCompletionData
    {
        get => this.contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this.contractionCompletionData, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS station.
    /// </summary>
    public AtisStation? SelectedStation
    {
        get => this.selectedStation;
        set => this.RaiseAndSetIfChanged(ref this.selectedStation, value);
    }

    /// <summary>
    /// Gets or sets the selected ATIS preset.
    /// </summary>
    public AtisPreset? SelectedPreset
    {
        get => this.selectedPreset;
        set => this.RaiseAndSetIfChanged(ref this.selectedPreset, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use an external ATIS generator.
    /// </summary>
    public bool UseExternalAtisGenerator
    {
        get => this.useExternalAtisGenerator;
        set
        {
            this.RaiseAndSetIfChanged(ref this.useExternalAtisGenerator, value);
            if (!this.initializedProperties.Add(nameof(this.UseExternalAtisGenerator)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator URL.
    /// </summary>
    public string? ExternalGeneratorUrl
    {
        get => this.externalGeneratorUrl;
        set
        {
            this.RaiseAndSetIfChanged(ref this.externalGeneratorUrl, value);
            if (!this.initializedProperties.Add(nameof(this.ExternalGeneratorUrl)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator arrival runways.
    /// </summary>
    public string? ExternalGeneratorArrivalRunways
    {
        get => this.externalGeneratorArrivalRunways;
        set
        {
            this.RaiseAndSetIfChanged(ref this.externalGeneratorArrivalRunways, value);
            if (!this.initializedProperties.Add(nameof(this.ExternalGeneratorArrivalRunways)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator departure runways.
    /// </summary>
    public string? ExternalGeneratorDepartureRunways
    {
        get => this.externalGeneratorDepartureRunways;
        set
        {
            this.RaiseAndSetIfChanged(ref this.externalGeneratorDepartureRunways, value);
            if (!this.initializedProperties.Add(nameof(this.ExternalGeneratorDepartureRunways)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator approaches.
    /// </summary>
    public string? ExternalGeneratorApproaches
    {
        get => this.externalGeneratorApproaches;
        set
        {
            this.RaiseAndSetIfChanged(ref this.externalGeneratorApproaches, value);
            if (!this.initializedProperties.Add(nameof(this.ExternalGeneratorApproaches)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator remarks.
    /// </summary>
    public string? ExternalGeneratorRemarks
    {
        get => this.externalGeneratorRemarks;
        set
        {
            this.RaiseAndSetIfChanged(ref this.externalGeneratorRemarks, value);
            if (!this.initializedProperties.Add(nameof(this.ExternalGeneratorRemarks)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    /// <summary>
    /// Gets or sets the external generator sandbox response.
    /// </summary>
    public string? ExternalGeneratorSandboxResponse
    {
        get => this.externalGeneratorSandboxResponse;
        set => this.RaiseAndSetIfChanged(ref this.externalGeneratorSandboxResponse, value);
    }

    /// <summary>
    /// Gets or sets the ATIS template text.
    /// </summary>
    public string AtisTemplateText
    {
        get => this.atisTemplateTextDocument?.Text ?? string.Empty;
        set => this.AtisTemplateTextDocument = new TextDocument(value);
    }

    /// <summary>
    /// Gets or sets the ATIS template text document.
    /// </summary>
    public TextDocument? AtisTemplateTextDocument
    {
        get => this.atisTemplateTextDocument;
        set => this.RaiseAndSetIfChanged(ref this.atisTemplateTextDocument, value);
    }

    /// <summary>
    /// Gets or sets the sandbox METAR.
    /// </summary>
    public string? SandboxMetar
    {
        get => this.sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref this.sandboxMetar, value);
    }

    /// <summary>
    /// Applies the configuration changes for the currently selected preset and updates the session profile if applicable.
    /// </summary>
    /// <returns>
    /// A boolean value indicating whether the configuration was successfully applied. Returns true if successful; otherwise, false.
    /// </returns>
    public bool ApplyConfig()
    {
        if (this.SelectedPreset == null)
        {
            return true;
        }

        if (this.SelectedPreset.Template != this.AtisTemplateText)
        {
            this.SelectedPreset.Template = this.AtisTemplateText;
        }

        if (this.SelectedPreset is { ExternalGenerator: not null })
        {
            if (this.SelectedPreset.ExternalGenerator.Enabled != this.UseExternalAtisGenerator)
            {
                this.SelectedPreset.ExternalGenerator.Enabled = this.UseExternalAtisGenerator;
            }

            if (this.SelectedPreset.ExternalGenerator.Url != this.ExternalGeneratorUrl)
            {
                this.SelectedPreset.ExternalGenerator.Url = this.ExternalGeneratorUrl;
            }

            if (this.SelectedPreset.ExternalGenerator.Arrival != this.ExternalGeneratorArrivalRunways)
            {
                this.SelectedPreset.ExternalGenerator.Arrival = this.ExternalGeneratorArrivalRunways;
            }

            if (this.SelectedPreset.ExternalGenerator.Departure != this.ExternalGeneratorDepartureRunways)
            {
                this.SelectedPreset.ExternalGenerator.Departure = this.ExternalGeneratorDepartureRunways;
            }

            if (this.SelectedPreset.ExternalGenerator.Approaches != this.ExternalGeneratorApproaches)
            {
                this.SelectedPreset.ExternalGenerator.Approaches = this.ExternalGeneratorApproaches;
            }

            if (this.SelectedPreset.ExternalGenerator.Remarks != this.ExternalGeneratorRemarks)
            {
                this.SelectedPreset.ExternalGenerator.Remarks = this.ExternalGeneratorRemarks;
            }
        }

        if (this.HasErrors)
        {
            return false;
        }

        if (this.sessionManager.CurrentProfile != null)
        {
            this.profileRepository.Save(this.sessionManager.CurrentProfile);
        }

        this.HasUnsavedChanges = false;

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
        if (this.SelectedStation == null || string.IsNullOrEmpty(this.SelectedStation.Identifier))
        {
            return;
        }

        var metar = await this.metarRepository.GetMetar(
            this.SelectedStation.Identifier,
            false,
            false);
        this.SandboxMetar = metar?.RawMetar;
    }

    private async Task HandleTestExternalGenerator()
    {
        if (!string.IsNullOrEmpty(this.ExternalGeneratorUrl))
        {
            var url = this.ExternalGeneratorUrl;
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "http://" + url;
            }

            url = url.Replace("$metar", HttpUtility.UrlEncode(this.SandboxMetar));
            url = url.Replace("$arrrwy", this.ExternalGeneratorArrivalRunways);
            url = url.Replace("$deprwy", this.ExternalGeneratorDepartureRunways);
            url = url.Replace("$app", this.ExternalGeneratorApproaches);
            url = url.Replace("$remarks", this.ExternalGeneratorRemarks);
            url = url.Replace("$atiscode", StringExtensions.RandomLetter());

            var response = await this.downloader.DownloadStringAsync(url);

            response = Regex.Replace(response, @"\[(.*?)\]", " $1 ");
            response = Regex.Replace(response, @"\s+", " ");

            this.ExternalGeneratorSandboxResponse = response.Trim();
        }
    }

    private async Task HandleOpenSortPresetsDialog()
    {
        if (this.DialogOwner == null)
        {
            return;
        }

        if (this.SelectedStation == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var dialog = this.windowFactory.CreateSortPresetsDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is SortPresetsDialogViewModel context)
            {
                context.Presets = new ObservableCollection<AtisPreset>(this.SelectedStation.Presets);

                context.Presets.CollectionChanged += (_, _) =>
                {
                    var idx = 0;
                    this.SelectedStation.Presets.Clear();
                    foreach (var item in context.Presets)
                    {
                        item.Ordinal = ++idx;
                        this.SelectedStation.Presets.Add(item);
                    }

                    if (this.sessionManager.CurrentProfile != null)
                    {
                        this.profileRepository.Save(this.sessionManager.CurrentProfile);
                    }

                    this.RefreshPresetList();
                };

                await dialog.ShowDialog((Window)this.DialogOwner);
            }
        }
    }

    private async Task HandleDeletePreset()
    {
        if (this.DialogOwner == null)
        {
            return;
        }

        if (this.SelectedStation != null && this.SelectedPreset != null)
        {
            if (await MessageBox.ShowDialog(
                    (Window)this.DialogOwner,
                    "Are you sure you want to delete the selected Preset? This action cannot be undone.",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information) == MessageBoxResult.Yes)
            {
                this.SelectedStation.Presets.Remove(this.SelectedPreset);
                if (this.sessionManager.CurrentProfile != null)
                {
                    this.profileRepository.Save(this.sessionManager.CurrentProfile);
                }

                this.RefreshPresetList();
            }
        }
    }

    private async Task HandleCopyPreset()
    {
        if (this.SelectedStation == null || this.SelectedPreset == null)
        {
            return;
        }

        if (this.DialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousValue = this.SelectedPreset.Name;

            var dialog = this.windowFactory.CreateUserInputDialog();
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
                            if (this.SelectedStation.Presets.Any(x => x.Name == context.UserValue))
                            {
                                context.SetError(
                                    "Another preset already exists with that name. " +
                                    "Please choose a new name.");
                                return;
                            }

                            var copy = this.SelectedPreset.Clone();
                            copy.Ordinal = this.SelectedStation.Presets.Select(x => x.Ordinal).Max() + 1;
                            copy.Name = context.UserValue.Trim();
                            this.SelectedStation.Presets.Add(copy);
                            if (this.sessionManager.CurrentProfile != null)
                            {
                                this.profileRepository.Save(this.sessionManager.CurrentProfile);
                            }

                            this.RefreshPresetList();
                            context.ClearError();
                        }
                    }
                };
            }

            await dialog.ShowDialog((Window)this.DialogOwner);
        }
    }

    private async Task HandleRenamePreset()
    {
        if (this.SelectedStation == null || this.SelectedPreset == null)
        {
            return;
        }

        if (this.DialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousValue = this.SelectedPreset.Name;

            var dialog = this.windowFactory.CreateUserInputDialog();
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
                            if (this.SelectedStation.Presets.Any(
                                    x => x.Name == context.UserValue && x != this.SelectedPreset))
                            {
                                context.SetError(
                                    "Another preset already exists with that name. Please choose a new name.");
                                return;
                            }

                            this.SelectedPreset.Name = context.UserValue.Trim();
                            if (this.sessionManager.CurrentProfile != null)
                            {
                                this.profileRepository.Save(this.sessionManager.CurrentProfile);
                            }

                            this.RefreshPresetList();

                            context.ClearError();
                        }
                    }
                };
            }

            await dialog.ShowDialog((Window)this.DialogOwner);
        }
    }

    private void HandleSelectedPresetChanged(AtisPreset? preset)
    {
        if (preset == null)
        {
            return;
        }

        this.SelectedPreset = preset;

        this.UseExternalAtisGenerator = false;
        this.ExternalGeneratorUrl = null;
        this.ExternalGeneratorArrivalRunways = null;
        this.ExternalGeneratorDepartureRunways = null;
        this.ExternalGeneratorApproaches = null;
        this.ExternalGeneratorRemarks = null;
        this.ExternalGeneratorSandboxResponse = null;
        this.AtisTemplateText = this.SelectedPreset?.Template ?? string.Empty;

        if (this.SelectedPreset?.ExternalGenerator != null)
        {
            this.UseExternalAtisGenerator = this.SelectedPreset.ExternalGenerator.Enabled;
            this.ExternalGeneratorUrl = this.SelectedPreset.ExternalGenerator.Url;
            this.ExternalGeneratorArrivalRunways = this.SelectedPreset.ExternalGenerator.Arrival;
            this.ExternalGeneratorDepartureRunways = this.SelectedPreset.ExternalGenerator.Departure;
            this.ExternalGeneratorApproaches = this.SelectedPreset.ExternalGenerator.Approaches;
            this.ExternalGeneratorRemarks = this.SelectedPreset.ExternalGenerator.Remarks;
        }

        this.HasUnsavedChanges = false;
    }

    private async Task HandleNewPreset()
    {
        if (this.SelectedStation == null)
        {
            return;
        }

        if (this.DialogOwner == null)
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

            var dialog = this.windowFactory.CreateUserInputDialog();
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
                            if (this.SelectedStation.Presets.Any(x => x.Name == context.UserValue))
                            {
                                context.SetError(
                                    "Another preset already exists with that name. Please choose a new name.");
                                return;
                            }

                            var preset = new AtisPreset
                            {
                                Ordinal = this.SelectedStation.Presets.Any()
                                    ? this.SelectedStation.Presets.Select(x => x.Ordinal).Max() + 1
                                    : 0,
                                Name = context.UserValue.Trim(),
                                Template = "[FACILITY] ATIS INFO [ATIS_CODE] [TIME]. [WX]. [ARPT_COND] [NOTAMS]",
                            };
                            this.SelectedStation.Presets.Add(preset);
                            if (this.sessionManager.CurrentProfile != null)
                            {
                                this.profileRepository.Save(this.sessionManager.CurrentProfile);
                            }

                            this.RefreshPresetList();

                            context.ClearError();
                        }
                    }
                };
            }

            await dialog.ShowDialog((Window)this.DialogOwner);
        }
    }

    private void RefreshPresetList()
    {
        if (this.SelectedStation != null)
        {
            this.SelectedPreset = null;
            this.UseExternalAtisGenerator = false;
            this.AtisTemplateText = string.Empty;
            this.Presets = new ObservableCollection<AtisPreset>(this.SelectedStation.Presets);
            MessageBus.Current.SendMessage(new StationPresetsChanged(this.SelectedStation.Id));
            this.HasUnsavedChanges = false;
        }
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this.SelectedStation = station;
        this.Presets = new ObservableCollection<AtisPreset>(station.Presets);
        this.UseExternalAtisGenerator = false;
        this.ExternalGeneratorUrl = null;
        this.ExternalGeneratorArrivalRunways = null;
        this.ExternalGeneratorDepartureRunways = null;
        this.ExternalGeneratorApproaches = null;
        this.ExternalGeneratorRemarks = null;
        this.ExternalGeneratorSandboxResponse = null;
        this.AtisTemplateText = string.Empty;

        this.ContractionCompletionData = [];
        foreach (var contraction in station.Contractions)
        {
            if (!string.IsNullOrEmpty(contraction.VariableName) && !string.IsNullOrEmpty(contraction.Voice))
            {
                this.ContractionCompletionData.Add(new AutoCompletionData(contraction.VariableName, contraction.Voice));
            }
        }

        this.HasUnsavedChanges = false;
    }
}
