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

public class PresetsViewModel : ReactiveViewModelBase
{
    private readonly IDownloader _downloader;
    private readonly HashSet<string> _initializedProperties = [];
    private readonly IMetarRepository _metarRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;

    public PresetsViewModel(
        IWindowFactory windowFactory,
        IDownloader downloader,
        IMetarRepository metarRepository,
        IProfileRepository profileRepository,
        ISessionManager sessionManager)
    {
        this._windowFactory = windowFactory;
        this._downloader = downloader;
        this._metarRepository = metarRepository;
        this._profileRepository = profileRepository;
        this._sessionManager = sessionManager;

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

    public IDialogOwner? DialogOwner { get; set; }

    public ReactiveCommand<AtisStation, Unit> AtisStationChanged { get; }

    public ReactiveCommand<AtisPreset, Unit> SelectedPresetChanged { get; }

    public ReactiveCommand<Unit, Unit> NewPresetCommand { get; }

    public ReactiveCommand<Unit, Unit> RenamePresetCommand { get; }

    public ReactiveCommand<Unit, Unit> CopyPresetCommand { get; }

    public ReactiveCommand<Unit, Unit> DeletePresetCommand { get; }

    public ReactiveCommand<Unit, Unit> OpenSortPresetsDialogCommand { get; }

    public ReactiveCommand<Unit, Unit> TestExternalGeneratorCommand { get; }

    public ReactiveCommand<string, Unit> TemplateVariableClicked { get; }

    public ReactiveCommand<Unit, Unit> FetchSandboxMetarCommand { get; }

    private async Task HandleFetchSandboxMetar()
    {
        if (this.SelectedStation == null || string.IsNullOrEmpty(this.SelectedStation.Identifier))
        {
            return;
        }

        var metar = await this._metarRepository.GetMetar(
            this.SelectedStation.Identifier,
            false,
            false);
        this.SandboxMetar = metar?.RawMetar;
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

            variable = variable?.Replace("__", "_") ?? "";

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

        if (this._sessionManager.CurrentProfile != null)
        {
            this._profileRepository.Save(this._sessionManager.CurrentProfile);
        }

        this.HasUnsavedChanges = false;

        return true;
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

            var response = await this._downloader.DownloadStringAsync(url);

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

            var dialog = this._windowFactory.CreateSortPresetsDialog();
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

                    if (this._sessionManager.CurrentProfile != null)
                    {
                        this._profileRepository.Save(this._sessionManager.CurrentProfile);
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
                if (this._sessionManager.CurrentProfile != null)
                {
                    this._profileRepository.Save(this._sessionManager.CurrentProfile);
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

            var dialog = this._windowFactory.CreateUserInputDialog();
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
                            if (this._sessionManager.CurrentProfile != null)
                            {
                                this._profileRepository.Save(this._sessionManager.CurrentProfile);
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

            var dialog = this._windowFactory.CreateUserInputDialog();
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
                            if (this._sessionManager.CurrentProfile != null)
                            {
                                this._profileRepository.Save(this._sessionManager.CurrentProfile);
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
        this.AtisTemplateText = this.SelectedPreset?.Template ?? "";

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

            var previousValue = "";

            var dialog = this._windowFactory.CreateUserInputDialog();
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
                                Template = "[FACILITY] ATIS INFO [ATIS_CODE] [TIME]. [WX]. [ARPT_COND] [NOTAMS]"
                            };
                            this.SelectedStation.Presets.Add(preset);
                            if (this._sessionManager.CurrentProfile != null)
                            {
                                this._profileRepository.Save(this._sessionManager.CurrentProfile);
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
            this.AtisTemplateText = "";
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
        this.AtisTemplateText = "";

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

    #region Reactive Properties

    private bool _hasUnsavedChanges;

    public bool HasUnsavedChanges
    {
        get => this._hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref this._hasUnsavedChanges, value);
    }

    private ObservableCollection<AtisPreset>? _presets;

    public ObservableCollection<AtisPreset>? Presets
    {
        get => this._presets;
        set => this.RaiseAndSetIfChanged(ref this._presets, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];

    public List<ICompletionData> ContractionCompletionData
    {
        get => this._contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref this._contractionCompletionData, value);
    }

    private AtisStation? _selectedStation;

    private AtisStation? SelectedStation
    {
        get => this._selectedStation;
        set => this.RaiseAndSetIfChanged(ref this._selectedStation, value);
    }

    private AtisPreset? _selectedPreset;

    public AtisPreset? SelectedPreset
    {
        get => this._selectedPreset;
        set => this.RaiseAndSetIfChanged(ref this._selectedPreset, value);
    }

    private bool _useExternalAtisGenerator;

    public bool UseExternalAtisGenerator
    {
        get => this._useExternalAtisGenerator;
        set
        {
            this.RaiseAndSetIfChanged(ref this._useExternalAtisGenerator, value);
            if (!this._initializedProperties.Add(nameof(this.UseExternalAtisGenerator)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _externalGeneratorUrl;

    public string? ExternalGeneratorUrl
    {
        get => this._externalGeneratorUrl;
        set
        {
            this.RaiseAndSetIfChanged(ref this._externalGeneratorUrl, value);
            if (!this._initializedProperties.Add(nameof(this.ExternalGeneratorUrl)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _externalGeneratorArrivalRunways;

    public string? ExternalGeneratorArrivalRunways
    {
        get => this._externalGeneratorArrivalRunways;
        set
        {
            this.RaiseAndSetIfChanged(ref this._externalGeneratorArrivalRunways, value);
            if (!this._initializedProperties.Add(nameof(this.ExternalGeneratorArrivalRunways)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _externalGeneratorDepartureRunways;

    public string? ExternalGeneratorDepartureRunways
    {
        get => this._externalGeneratorDepartureRunways;
        set
        {
            this.RaiseAndSetIfChanged(ref this._externalGeneratorDepartureRunways, value);
            if (!this._initializedProperties.Add(nameof(this.ExternalGeneratorDepartureRunways)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _externalGeneratorApproaches;

    public string? ExternalGeneratorApproaches
    {
        get => this._externalGeneratorApproaches;
        set
        {
            this.RaiseAndSetIfChanged(ref this._externalGeneratorApproaches, value);
            if (!this._initializedProperties.Add(nameof(this.ExternalGeneratorApproaches)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _externalGeneratorRemarks;

    public string? ExternalGeneratorRemarks
    {
        get => this._externalGeneratorRemarks;
        set
        {
            this.RaiseAndSetIfChanged(ref this._externalGeneratorRemarks, value);
            if (!this._initializedProperties.Add(nameof(this.ExternalGeneratorRemarks)))
            {
                this.HasUnsavedChanges = true;
            }
        }
    }

    private string? _externalGeneratorSandboxResponse;

    public string? ExternalGeneratorSandboxResponse
    {
        get => this._externalGeneratorSandboxResponse;
        set => this.RaiseAndSetIfChanged(ref this._externalGeneratorSandboxResponse, value);
    }

    private string AtisTemplateText
    {
        get => this._atisTemplateTextDocument?.Text ?? "";
        set => this.AtisTemplateTextDocument = new TextDocument(value);
    }

    private TextDocument? _atisTemplateTextDocument = new();

    public TextDocument? AtisTemplateTextDocument
    {
        get => this._atisTemplateTextDocument;
        set => this.RaiseAndSetIfChanged(ref this._atisTemplateTextDocument, value);
    }

    private string? _sandboxMetar;

    public string? SandboxMetar
    {
        get => this._sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref this._sandboxMetar, value);
    }

    #endregion
}