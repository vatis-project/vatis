using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
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
    private readonly HashSet<string> _initializedProperties = [];
    private readonly IMetarRepository _metarRepository;
    private readonly IDownloader _downloader;
    private readonly IWindowFactory _windowFactory;
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;

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

    #region Reactive Properties
    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    private ObservableCollection<AtisPreset>? _presets;
    public ObservableCollection<AtisPreset>? Presets
    {
        get => _presets;
        set => this.RaiseAndSetIfChanged(ref _presets, value);
    }

    private List<ICompletionData> _contractionCompletionData = [];
    public List<ICompletionData> ContractionCompletionData
    {
        get => _contractionCompletionData;
        set => this.RaiseAndSetIfChanged(ref _contractionCompletionData, value);
    }

    private AtisStation? _selectedStation;
    private AtisStation? SelectedStation
    {
        get => _selectedStation;
        set => this.RaiseAndSetIfChanged(ref _selectedStation, value);
    }

    private AtisPreset? _selectedPreset;
    public AtisPreset? SelectedPreset
    {
        get => _selectedPreset;
        set => this.RaiseAndSetIfChanged(ref _selectedPreset, value);
    }

    private bool _useExternalAtisGenerator;
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

    private string? _externalGeneratorUrl;
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

    private string? _externalGeneratorArrivalRunways;
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

    private string? _externalGeneratorDepartureRunways;
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

    private string? _externalGeneratorApproaches;
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

    private string? _externalGeneratorRemarks;
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

    private string? _externalGeneratorSandboxResponse;
    public string? ExternalGeneratorSandboxResponse
    {
        get => _externalGeneratorSandboxResponse;
        set => this.RaiseAndSetIfChanged(ref _externalGeneratorSandboxResponse, value);
    }

    private string AtisTemplateText
    {
        get => _atisTemplateTextDocument?.Text ?? "";
        set => AtisTemplateTextDocument = new TextDocument(value);
    }

    private TextDocument? _atisTemplateTextDocument = new();
    public TextDocument? AtisTemplateTextDocument
    {
        get => _atisTemplateTextDocument;
        set => this.RaiseAndSetIfChanged(ref _atisTemplateTextDocument, value);
    }

    private string? _sandboxMetar;
    public string? SandboxMetar
    {
        get => _sandboxMetar;
        set => this.RaiseAndSetIfChanged(ref _sandboxMetar, value);
    }
    #endregion

    public PresetsViewModel(IWindowFactory windowFactory,
        IDownloader downloader, IMetarRepository metarRepository, IProfileRepository profileRepository,
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
    }

    private async Task HandleFetchSandboxMetar()
    {
        if (SelectedStation == null || string.IsNullOrEmpty(SelectedStation.Identifier))
            return;

        var metar = await _metarRepository.GetMetar(SelectedStation.Identifier, monitor: false,
            triggerMessageBus: false);
        SandboxMetar = metar?.RawMetar;
    }

    private static void HandleTemplateVariableClicked(string? variable)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var topLevel = TopLevel.GetTopLevel(lifetime.MainWindow);
            var focusedElement = topLevel?.FocusManager?.GetFocusedElement();

            variable = variable?.Replace("__", "_") ?? "";

            if (focusedElement is TemplateVariableTextBox focusedTextBox)
            {
                var caretIndex = focusedTextBox.CaretIndex;
                focusedTextBox.Text = focusedTextBox.Text?.Insert(focusedTextBox.CaretIndex, variable);
                focusedTextBox.CaretIndex = (variable.Length) + caretIndex;
            }

            if (focusedElement is AvaloniaEdit.Editing.TextArea focusedTextEditor)
            {
                var caretIndex = focusedTextEditor.Caret.Offset;
                focusedTextEditor.Document.Text =
                    focusedTextEditor.Document.Text.Insert(focusedTextEditor.Caret.Offset, variable);
                focusedTextEditor.Caret.Offset = (variable.Length) + caretIndex;
            }
        }
    }

    public bool ApplyConfig()
    {
        if (SelectedPreset == null)
            return true;

        if (SelectedPreset.Template != AtisTemplateText)
            SelectedPreset.Template = AtisTemplateText;

        if (SelectedPreset is { ExternalGenerator: not null })
        {
            if (SelectedPreset.ExternalGenerator.Enabled != UseExternalAtisGenerator)
                SelectedPreset.ExternalGenerator.Enabled = UseExternalAtisGenerator;

            if (SelectedPreset.ExternalGenerator.Url != ExternalGeneratorUrl)
                SelectedPreset.ExternalGenerator.Url = ExternalGeneratorUrl;

            if (SelectedPreset.ExternalGenerator.Arrival != ExternalGeneratorArrivalRunways)
                SelectedPreset.ExternalGenerator.Arrival = ExternalGeneratorArrivalRunways;

            if (SelectedPreset.ExternalGenerator.Departure != ExternalGeneratorDepartureRunways)
                SelectedPreset.ExternalGenerator.Departure = ExternalGeneratorDepartureRunways;

            if (SelectedPreset.ExternalGenerator.Approaches != ExternalGeneratorApproaches)
                SelectedPreset.ExternalGenerator.Approaches = ExternalGeneratorApproaches;

            if (SelectedPreset.ExternalGenerator.Remarks != ExternalGeneratorRemarks)
                SelectedPreset.ExternalGenerator.Remarks = ExternalGeneratorRemarks;
        }

        if (HasErrors)
            return false;

        if (_sessionManager.CurrentProfile != null)
            _profileRepository.Save(_sessionManager.CurrentProfile);

        HasUnsavedChanges = false;

        return true;
    }

    private async Task HandleTestExternalGenerator()
    {
        if (!string.IsNullOrEmpty(ExternalGeneratorUrl))
        {
            var url = ExternalGeneratorUrl;
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                url = "http://" + url;

            url = url.Replace("$metar", System.Web.HttpUtility.UrlEncode(SandboxMetar));
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
            return;

        if (SelectedStation == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

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
                        _profileRepository.Save(_sessionManager.CurrentProfile);

                    RefreshPresetList();
                };

                await dialog.ShowDialog((Window)DialogOwner);
            }
        }
    }

    private async Task HandleDeletePreset()
    {
        if (DialogOwner == null)
            return;

        if (SelectedStation != null && SelectedPreset != null)
        {
            if (await MessageBox.ShowDialog((Window)DialogOwner,
                    "Are you sure you want to delete the selected Preset? This action cannot be undone.", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
            {
                SelectedStation.Presets.Remove(SelectedPreset);
                if (_sessionManager.CurrentProfile != null)
                    _profileRepository.Save(_sessionManager.CurrentProfile);
                RefreshPresetList();
            }
        }
    }

    private async Task HandleCopyPreset()
    {
        if (SelectedStation == null || SelectedPreset == null)
            return;

        if (DialogOwner == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

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
                                context.SetError("Another preset already exists with that name. " +
                                                 "Please choose a new name.");
                                return;
                            }

                            var copy = SelectedPreset.Clone();
                            copy.Ordinal = SelectedStation.Presets.Select(x => x.Ordinal).Max() + 1;
                            copy.Name = context.UserValue.Trim();
                            SelectedStation.Presets.Add(copy);
                            if (_sessionManager.CurrentProfile != null)
                                _profileRepository.Save(_sessionManager.CurrentProfile);
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
            return;

        if (DialogOwner == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

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
                            if (SelectedStation.Presets.Any(x => x.Name == context.UserValue && x != SelectedPreset))
                            {
                                context.SetError(
                                    "Another preset already exists with that name. Please choose a new name.");
                                return;
                            }

                            SelectedPreset.Name = context.UserValue.Trim();
                            if (_sessionManager.CurrentProfile != null)
                                _profileRepository.Save(_sessionManager.CurrentProfile);
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
            return;

        SelectedPreset = preset;

        UseExternalAtisGenerator = false;
        ExternalGeneratorUrl = null;
        ExternalGeneratorArrivalRunways = null;
        ExternalGeneratorDepartureRunways = null;
        ExternalGeneratorApproaches = null;
        ExternalGeneratorRemarks = null;
        ExternalGeneratorSandboxResponse = null;
        AtisTemplateText = SelectedPreset?.Template ?? "";

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
            return;

        if (DialogOwner == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var previousValue = "";

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
                                Template = "[FACILITY] ATIS INFO [ATIS_CODE] [TIME]. [WX]. [ARPT_COND] [NOTAMS]"
                            };
                            SelectedStation.Presets.Add(preset);
                            if (_sessionManager.CurrentProfile != null)
                                _profileRepository.Save(_sessionManager.CurrentProfile);
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
            AtisTemplateText = "";
            Presets = new ObservableCollection<AtisPreset>(SelectedStation.Presets);
            MessageBus.Current.SendMessage(new StationPresetsChanged(SelectedStation.Id));
            HasUnsavedChanges = false;
        }
    }

    private void HandleUpdateProperties(AtisStation? station)
    {
        if (station == null)
            return;

        SelectedStation = station;
        Presets = new ObservableCollection<AtisPreset>(station.Presets);
        UseExternalAtisGenerator = false;
        ExternalGeneratorUrl = null;
        ExternalGeneratorArrivalRunways = null;
        ExternalGeneratorDepartureRunways = null;
        ExternalGeneratorApproaches = null;
        ExternalGeneratorRemarks = null;
        ExternalGeneratorSandboxResponse = null;
        AtisTemplateText = "";

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
