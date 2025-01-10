using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Config;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;
using Vatsim.Vatis.Ui.Windows;
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Voice.Audio;

namespace Vatsim.Vatis.Ui.ViewModels;

public class AtisConfigurationWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig _appConfig;
    private readonly CompositeDisposable _disposables = new();
    private readonly INavDataRepository _navDataRepository;
    private readonly IProfileRepository _profileRepository;
    private readonly ISessionManager _sessionManager;
    private readonly ITextToSpeechService _textToSpeechService;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IWindowFactory _windowFactory;
    private IDialogOwner? _dialogOwner;

    public AtisConfigurationWindowViewModel(
        IAppConfig appConfig,
        ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IViewModelFactory viewModelFactory,
        ITextToSpeechService textToSpeechService,
        INavDataRepository navDataRepository,
        IProfileRepository profileRepository)
    {
        this._appConfig = appConfig;
        this._sessionManager = sessionManager;
        this._windowFactory = windowFactory;
        this._viewModelFactory = viewModelFactory;
        this._textToSpeechService = textToSpeechService;
        this._navDataRepository = navDataRepository;
        this._profileRepository = profileRepository;

        this.CloseWindowCommand = ReactiveCommand.CreateFromTask<ICloseable>(this.HandleCloseWindow);
        this.SelectedAtisStationChanged = ReactiveCommand.Create<AtisStation>(this.HandleSelectedAtisStationChanged);
        this.SaveAndCloseCommand = ReactiveCommand.Create<ICloseable>(this.HandleSaveAndClose);
        this.ApplyChangesCommand = ReactiveCommand.Create(
            this.HandleApplyChanges,
            this.WhenAnyValue(x => x.HasUnsavedChanges));
        this.CancelChangesCommand = ReactiveCommand.CreateFromTask<ICloseable>(this.HandleCancelChanges);
        this.NewAtisStationDialogCommand = ReactiveCommand.CreateFromTask(this.HandleNewAtisStationDialog);
        this.ExportAtisCommand = ReactiveCommand.CreateFromTask(this.HandleExportAtisStation);
        this.DeleteAtisCommand = ReactiveCommand.CreateFromTask(this.HandleDeleteAtis);
        this.RenameAtisCommand = ReactiveCommand.CreateFromTask(this.HandleRenameAtis);
        this.CopyAtisCommand = ReactiveCommand.CreateFromTask(this.HandleCopyAtis);
        this.ImportAtisStationCommand = ReactiveCommand.Create(this.HandleImportAtisStation);

        this._disposables.Add(this.CloseWindowCommand);
        this._disposables.Add(this.SaveAndCloseCommand);
        this._disposables.Add(this.ApplyChangesCommand);
        this._disposables.Add(this.CancelChangesCommand);
        this._disposables.Add(this.NewAtisStationDialogCommand);
        this._disposables.Add(this.ExportAtisCommand);
        this._disposables.Add(this.DeleteAtisCommand);
        this._disposables.Add(this.RenameAtisCommand);
        this._disposables.Add(this.CopyAtisCommand);
        this._disposables.Add(this.ImportAtisStationCommand);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                this.ShowOverlay = lifetime.Windows.Count(w => w.GetType() != typeof(MainWindow)) > 1;
            };
        }

        if (this._sessionManager.CurrentProfile?.Stations != null)
        {
            foreach (var station in this._sessionManager.CurrentProfile.Stations)
            {
                this._atisStationSource.Add(station);
            }
        }

        this._atisStationSource.Connect()
            .AutoRefresh(x => x.Name)
            .AutoRefresh(x => x.AtisType)
            .Sort(
                SortExpressionComparer<AtisStation>
                    .Ascending(i => i.Identifier)
                    .ThenBy(i => i.AtisType))
            .Bind(out var sortedStations)
            .Subscribe(_ => { this.AtisStations = sortedStations; });
        this.AtisStations = sortedStations;
    }

    public GeneralConfigViewModel? GeneralConfigViewModel { get; private set; }

    public PresetsViewModel? PresetsViewModel { get; private set; }

    public FormattingViewModel? FormattingViewModel { get; private set; }

    public ContractionsViewModel? ContractionsViewModel { get; private set; }

    public SandboxViewModel? SandboxViewModel { get; private set; }

    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    public ReactiveCommand<AtisStation, Unit> SelectedAtisStationChanged { get; }

    public ReactiveCommand<ICloseable, Unit> SaveAndCloseCommand { get; }

    public ReactiveCommand<Unit, Unit> ApplyChangesCommand { get; }

    public ReactiveCommand<ICloseable, Unit> CancelChangesCommand { get; }

    public ReactiveCommand<Unit, Unit> NewAtisStationDialogCommand { get; }

    public ReactiveCommand<Unit, Unit> ExportAtisCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteAtisCommand { get; }

    public ReactiveCommand<Unit, Unit> RenameAtisCommand { get; }

    public ReactiveCommand<Unit, Unit> CopyAtisCommand { get; }

    public ReactiveCommand<Unit, Unit> ImportAtisStationCommand { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this._disposables.Dispose();
    }

    private async Task HandleCloseWindow(ICloseable? window)
    {
        if (this._dialogOwner == null)
        {
            window?.Close();
            return;
        }

        NativeAudio.StopBufferPlayback();

        if (this.SelectedAtisStation != null && this.HasUnsavedChanges)
        {
            if (await MessageBox.ShowDialog(
                    (Window)this._dialogOwner,
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information) == MessageBoxResult.Yes)
            {
                window?.Close();
            }
        }
        else
        {
            window?.Close();
        }
    }

    public void Initialize(IDialogOwner dialogOwner)
    {
        this._dialogOwner = dialogOwner;

        this.GeneralConfigViewModel = this._viewModelFactory.CreateGeneralConfigViewModel();
        this.GeneralConfigViewModel.AvailableVoices =
            new ObservableCollection<VoiceMetaData>(this._textToSpeechService.VoiceList);
        this.GeneralConfigViewModel.WhenAnyValue(x => x.SelectedTabIndex)
            .Subscribe(idx => { this.SelectedTabControlTabIndex = idx; });
        this.GeneralConfigViewModel.WhenAnyValue(x => x.HasUnsavedChanges)
            .Subscribe(val => { this.HasUnsavedChanges = val; });

        this.PresetsViewModel = this._viewModelFactory.CreatePresetsViewModel();
        this.PresetsViewModel.DialogOwner = this._dialogOwner;
        this.PresetsViewModel.WhenAnyValue(x => x.HasUnsavedChanges)
            .Subscribe(val => { this.HasUnsavedChanges = val; });

        this.FormattingViewModel = this._viewModelFactory.CreateFormattingViewModel();
        this.FormattingViewModel.DialogOwner = this._dialogOwner;
        this.FormattingViewModel.WhenAnyValue(x => x.HasUnsavedChanges)
            .Subscribe(val => { this.HasUnsavedChanges = val; });

        this.ContractionsViewModel = this._viewModelFactory.CreateContractionsViewModel();
        this.ContractionsViewModel.SetDialogOwner(this._dialogOwner);

        this.SandboxViewModel = this._viewModelFactory.CreateSandboxViewModel();
        this.SandboxViewModel.DialogOwner = this._dialogOwner;
    }

    private async Task HandleCancelChanges(ICloseable window)
    {
        if (this._dialogOwner == null)
        {
            window.Close();
            return;
        }

        NativeAudio.StopBufferPlayback();

        if (this.SelectedAtisStation != null && this.HasUnsavedChanges)
        {
            if (await MessageBox.ShowDialog(
                    (Window)this._dialogOwner,
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information) == MessageBoxResult.Yes)
            {
                window.Close();
            }
        }
        else
        {
            window.Close();
        }
    }

    private async Task HandleDeleteAtis()
    {
        if (this.SelectedAtisStation == null)
        {
            return;
        }

        if (this._dialogOwner == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)this._dialogOwner,
                $"Are you sure you want to delete the selected ATIS Station? This action will also delete all associated ATIS presets.\r\n\r\n{this.SelectedAtisStation}",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (this._sessionManager.CurrentProfile?.Stations == null)
            {
                return;
            }

            MessageBus.Current.SendMessage(new AtisStationDeleted(this.SelectedAtisStation.Id));
            this._sessionManager.CurrentProfile.Stations.Remove(this.SelectedAtisStation);
            this._appConfig.SaveConfig();
            this._atisStationSource.Remove(this.SelectedAtisStation);
            this.SelectedAtisStation = null;
            this.ResetFields();
        }

        this.ClearAllErrors();
    }

    private async Task HandleCopyAtis()
    {
        if (this._dialogOwner == null)
        {
            return;
        }

        if (this.SelectedAtisStation == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousAirportIdentifier = "";
            var previousStationName = "";
            var previousAtisType = AtisType.Combined;

            var dialog = this._windowFactory.CreateNewAtisStationDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is NewAtisStationDialogViewModel context)
            {
                context.AirportIdentifier = previousAirportIdentifier;
                context.StationName = previousStationName;
                context.AtisType = previousAtisType;
                context.DialogResultChanged += async (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        previousAirportIdentifier = context.AirportIdentifier;
                        previousStationName = context.StationName;
                        previousAtisType = context.AtisType;

                        context.ClearAllErrors();

                        if (string.IsNullOrWhiteSpace(context.AirportIdentifier))
                        {
                            context.RaiseError("AirportIdentifier", "Airport Identifier is required.");
                        }
                        else if (this._navDataRepository.GetAirport(context.AirportIdentifier) == null)
                        {
                            context.RaiseError("AirportIdentifier", "Airport Identifier does not exist.");
                        }

                        if (string.IsNullOrWhiteSpace(context.StationName))
                        {
                            context.RaiseError("StationName", "Name is required.");
                        }

                        if (context.HasErrors)
                        {
                            return;
                        }

                        if (this._atisStationSource.Items.Any(
                                x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType))
                        {
                            context.RaiseError("Duplicate", "");

                            if (await MessageBox.ShowDialog(
                                    dialog,
                                    $"{context.AirportIdentifier} ({context.AtisType}) already exists.",
                                    "Error",
                                    MessageBoxButton.Ok,
                                    MessageBoxIcon.Information) == MessageBoxResult.Ok)
                            {
                                return;
                            }
                        }

                        if (this._sessionManager.CurrentProfile?.Stations != null)
                        {
                            var clone = this.SelectedAtisStation.Clone();
                            clone.Identifier = context.AirportIdentifier;
                            clone.Name = context.StationName;
                            clone.AtisType = context.AtisType;

                            this._sessionManager.CurrentProfile.Stations.Add(clone);
                            this._appConfig.SaveConfig();
                            this._atisStationSource.Add(clone);
                            this.SelectedAtisStation = clone;
                            MessageBus.Current.SendMessage(new AtisStationAdded(this.SelectedAtisStation.Id));
                        }
                    }
                };
                await dialog.ShowDialog((Window)this._dialogOwner);
            }
        }
    }

    private async void HandleImportAtisStation()
    {
        try
        {
            if (this._dialogOwner == null)
            {
                return;
            }

            var filters = new List<FilePickerFileType> { new("ATIS Station (*.station)") { Patterns = ["*.station"] } };
            var files = await FilePickerExtensions.OpenFilePickerAsync(filters, "Import ATIS Station");

            if (files == null)
            {
                return;
            }

            foreach (var file in files)
            {
                var fileContent = await File.ReadAllTextAsync(file);
                var station = JsonSerializer.Deserialize(fileContent, SourceGenerationContext.NewDefault.AtisStation);
                if (station != null)
                {
                    if (this._sessionManager.CurrentProfile?.Stations == null)
                    {
                        return;
                    }

                    if (this._sessionManager.CurrentProfile.Stations.Any(
                            x =>
                                x.Identifier == station.Identifier && x.AtisType == station.AtisType))
                    {
                        if (await MessageBox.ShowDialog(
                                (Window)this._dialogOwner,
                                $"{station.Identifier} ({station.AtisType}) already exists. Do you want to overwrite it?",
                                "Confirm",
                                MessageBoxButton.YesNo,
                                MessageBoxIcon.Information) == MessageBoxResult.Yes)
                        {
                            this._sessionManager.CurrentProfile?.Stations?.RemoveAll(
                                x =>
                                    x.Identifier == station.Identifier && x.AtisType == station.AtisType);
                        }
                        else
                        {
                            return;
                        }
                    }

                    this._sessionManager.CurrentProfile?.Stations?.Add(station);
                    this._appConfig.SaveConfig();
                    this._atisStationSource.Add(station);
                    MessageBus.Current.SendMessage(new AtisStationAdded(station.Id));
                }
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to import ATIS station");
        }
    }

    private async Task HandleRenameAtis()
    {
        if (this.SelectedAtisStation == null)
        {
            return;
        }

        if (this._dialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousValue = this.SelectedAtisStation.Name;

            var dialog = this._windowFactory.CreateUserInputDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is UserInputDialogViewModel context)
            {
                context.Prompt = "Name:";
                context.Title = "Rename ATIS Station";
                context.UserValue = previousValue;
                context.DialogResultChanged += (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        context.ClearError();

                        if (string.IsNullOrWhiteSpace(context.UserValue))
                        {
                            context.SetError("ATIS Station Name is required.");
                        }
                        else
                        {
                            this.SelectedAtisStation.Name = context.UserValue.Trim();
                            this._appConfig.SaveConfig();
                            this._atisStationSource.Edit(
                                list =>
                                {
                                    var item = list.FirstOrDefault(x => x.Id == this.SelectedAtisStation.Id);
                                    if (item != null)
                                    {
                                        item.Name = context.UserValue.Trim();
                                    }
                                });
                        }
                    }
                };
            }

            await dialog.ShowDialog((Window)this._dialogOwner);
        }
    }

    private async Task HandleExportAtisStation()
    {
        if (this.SelectedAtisStation == null)
        {
            return;
        }

        if (this._dialogOwner == null)
        {
            return;
        }

        var filters = new List<FilePickerFileType> { new("ATIS Station (*.station)") { Patterns = ["*.station"] } };
        var file = await FilePickerExtensions.SaveFileAsync(
            "Export ATIS Station",
            filters,
            $"{this.SelectedAtisStation.Name} ({this.SelectedAtisStation.AtisType})");

        if (file == null)
        {
            return;
        }

        this.SelectedAtisStation.Id = null!;

        await File.WriteAllTextAsync(
            file.Path.LocalPath,
            JsonSerializer.Serialize(this.SelectedAtisStation, SourceGenerationContext.NewDefault.AtisStation));
        await MessageBox.ShowDialog(
            (Window)this._dialogOwner,
            "ATIS Station successfully exported.",
            "Success",
            MessageBoxButton.Ok,
            MessageBoxIcon.Information);
    }

    private async Task HandleNewAtisStationDialog()
    {
        if (this._dialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousAirportIdentifier = "";
            var previousStationName = "";
            var previousAtisType = AtisType.Combined;
            var isDuplicateAcknowledged = false;

            var dialog = this._windowFactory.CreateNewAtisStationDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            if (dialog.DataContext is NewAtisStationDialogViewModel context)
            {
                context.AirportIdentifier = previousAirportIdentifier;
                context.StationName = previousStationName;
                context.AtisType = previousAtisType;
                context.DialogResultChanged += async (_, dialogResult) =>
                {
                    if (dialogResult == DialogResult.Ok)
                    {
                        previousAirportIdentifier = context.AirportIdentifier;
                        previousStationName = context.StationName;
                        previousAtisType = context.AtisType;

                        context.ClearAllErrors();

                        if (string.IsNullOrWhiteSpace(context.AirportIdentifier))
                        {
                            context.RaiseError("AirportIdentifier", "Airport Identifier is required.");
                        }
                        else if (this._navDataRepository.GetAirport(context.AirportIdentifier) == null)
                        {
                            context.RaiseError("AirportIdentifier", "Airport Identifier does not exist.");
                        }

                        if (string.IsNullOrWhiteSpace(context.StationName))
                        {
                            context.RaiseError("StationName", "Name is required.");
                        }

                        if (context.HasErrors)
                        {
                            return;
                        }

                        if (this._atisStationSource.Items.Any(
                                x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType))
                        {
                            if (!isDuplicateAcknowledged)
                            {
                                context.RaiseError("Duplicate", "");
                            }

                            if (await MessageBox.ShowDialog(
                                    dialog,
                                    $"{context.AirportIdentifier} ({context.AtisType}) already exists. Would you like to overwrite it?",
                                    "Error",
                                    MessageBoxButton.YesNo,
                                    MessageBoxIcon.Information) == MessageBoxResult.Yes)
                            {
                                isDuplicateAcknowledged = true;

                                context.ClearErrors("Duplicate");
                                context.OkButtonCommand.Execute(dialog).Subscribe();

                                this._sessionManager.CurrentProfile?.Stations?.RemoveAll(
                                    x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType);
                                this._appConfig.SaveConfig();
                            }
                            else
                            {
                                return;
                            }
                        }

                        if (this._sessionManager.CurrentProfile?.Stations != null)
                        {
                            var station = new AtisStation
                            {
                                Identifier = context.AirportIdentifier,
                                Name = context.StationName,
                                AtisType = context.AtisType
                            };
                            this._sessionManager.CurrentProfile.Stations.Add(station);
                            this._profileRepository.Save(this._sessionManager.CurrentProfile);

                            this._atisStationSource.Add(station);
                            this.SelectedAtisStation = station;

                            MessageBus.Current.SendMessage(new AtisStationAdded(station.Id));
                        }
                    }
                };
                await dialog.ShowDialog((Window)this._dialogOwner);
            }
        }
    }

    private void HandleSaveAndClose(ICloseable? window)
    {
        var general = this.GeneralConfigViewModel?.ApplyConfig();
        var presets = this.PresetsViewModel?.ApplyConfig();
        var formatting = this.FormattingViewModel?.ApplyChanges();
        var sandbox = this.SandboxViewModel?.ApplyConfig();

        if (general != null && presets != null && formatting != null && sandbox != null)
        {
            if (general.Value && presets.Value && formatting.Value && sandbox.Value)
            {
                if (this.SelectedAtisStation != null)
                {
                    MessageBus.Current.SendMessage(new AtisStationUpdated(this.SelectedAtisStation.Id));
                }

                window?.Close();
            }
        }
    }

    private void HandleApplyChanges()
    {
        this.GeneralConfigViewModel?.ApplyConfig();
        this.PresetsViewModel?.ApplyConfig();
        this.FormattingViewModel?.ApplyChanges();
        this.SandboxViewModel?.ApplyConfig();
    }

    private void HandleSelectedAtisStationChanged(AtisStation? station)
    {
        if (station == null)
        {
            return;
        }

        this.SelectedAtisStation = station;

        this.GeneralConfigViewModel?.AtisStationChanged.Execute(station).Subscribe();
        this.PresetsViewModel?.AtisStationChanged.Execute(station).Subscribe();
        this.FormattingViewModel?.AtisStationChanged.Execute(station).Subscribe();
        this.ContractionsViewModel?.AtisStationChanged.Execute(station).Subscribe();
        this.SandboxViewModel?.AtisStationChanged.Execute(station).Subscribe();
    }

    private void ResetFields()
    {
        this.GeneralConfigViewModel?.Reset();
        this.SelectedTabControlTabIndex = 0;
        this.HasUnsavedChanges = false;
    }

    #region Reactive UI Properties

    private readonly SourceList<AtisStation> _atisStationSource = new();

    public ReadOnlyObservableCollection<AtisStation> AtisStations { get; set; }

    private bool _hasUnsavedChanges;

    public bool HasUnsavedChanges
    {
        get => this._hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref this._hasUnsavedChanges, value);
    }

    private bool _showOverlay;

    public bool ShowOverlay
    {
        get => this._showOverlay;
        set => this.RaiseAndSetIfChanged(ref this._showOverlay, value);
    }

    private int _selectedTabControlTabIndex;

    public int SelectedTabControlTabIndex
    {
        get => this._selectedTabControlTabIndex;
        set => this.RaiseAndSetIfChanged(ref this._selectedTabControlTabIndex, value);
    }

    private AtisStation? _selectedAtisStation;

    public AtisStation? SelectedAtisStation
    {
        get => this._selectedAtisStation;
        set => this.RaiseAndSetIfChanged(ref this._selectedAtisStation, value);
    }

    #endregion
}