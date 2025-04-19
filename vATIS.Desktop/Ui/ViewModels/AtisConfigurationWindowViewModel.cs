// <copyright file="AtisConfigurationWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
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
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Ui.Dialogs;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Voice.Audio;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the ATIS configuration window.
/// </summary>
public class AtisConfigurationWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly INavDataRepository _navDataRepository;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly ITextToSpeechService _textToSpeechService;
    private readonly IProfileRepository _profileRepository;
    private readonly CompositeDisposable _disposables = [];
    private readonly SourceList<AtisStation> _atisStationSource = new();
    private bool _hasUnsavedChanges;
    private bool _requiresDisconnect;
    private bool _showOverlay;
    private int _selectedTabControlTabIndex;
    private AtisStation? _selectedAtisStation;
    private IDialogOwner? _dialogOwner;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisConfigurationWindowViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager instance.</param>
    /// <param name="windowFactory">The factory used to create application windows.</param>
    /// <param name="viewModelFactory">The factory used to create view models.</param>
    /// <param name="textToSpeechService">The text-to-speech service for the application.</param>
    /// <param name="navDataRepository">The navigation data repository.</param>
    /// <param name="profileRepository">The profile repository for managing user settings and profiles.</param>
    public AtisConfigurationWindowViewModel(
        ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IViewModelFactory viewModelFactory,
        ITextToSpeechService textToSpeechService,
        INavDataRepository navDataRepository,
        IProfileRepository profileRepository)
    {
        _sessionManager = sessionManager;
        _windowFactory = windowFactory;
        _viewModelFactory = viewModelFactory;
        _textToSpeechService = textToSpeechService;
        _navDataRepository = navDataRepository;
        _profileRepository = profileRepository;

        CloseWindowCommand = ReactiveCommand.CreateFromTask<ICloseable>(HandleCloseWindow);
        SelectedAtisStationChanged = ReactiveCommand.Create<AtisStation>(HandleSelectedAtisStationChanged);
        SaveAndCloseCommand = ReactiveCommand.Create<ICloseable>(HandleSaveAndClose);
        ApplyChangesCommand = ReactiveCommand.Create(HandleApplyChanges, this.WhenAnyValue(x => x.HasUnsavedChanges));
        CancelChangesCommand = ReactiveCommand.CreateFromTask<ICloseable>(HandleCancelChanges);
        NewAtisStationDialogCommand = ReactiveCommand.CreateFromTask(HandleNewAtisStationDialog);
        ExportAtisCommand = ReactiveCommand.CreateFromTask(HandleExportAtisStation);
        DeleteAtisCommand = ReactiveCommand.CreateFromTask(HandleDeleteAtis);
        RenameAtisCommand = ReactiveCommand.CreateFromTask(HandleRenameAtis);
        CopyAtisCommand = ReactiveCommand.CreateFromTask(HandleCopyAtis);
        ImportAtisStationCommand = ReactiveCommand.Create(HandleImportAtisStation);
        OpenSortAtisStationsDialogCommand = ReactiveCommand.CreateFromTask(HandleOpenSortAtisStationsDialog);

        _disposables.Add(CloseWindowCommand);
        _disposables.Add(SaveAndCloseCommand);
        _disposables.Add(ApplyChangesCommand);
        _disposables.Add(CancelChangesCommand);
        _disposables.Add(NewAtisStationDialogCommand);
        _disposables.Add(ExportAtisCommand);
        _disposables.Add(DeleteAtisCommand);
        _disposables.Add(RenameAtisCommand);
        _disposables.Add(CopyAtisCommand);
        _disposables.Add(ImportAtisStationCommand);
        _disposables.Add(OpenSortAtisStationsDialogCommand);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += OnCollectionChanged;
        }

        if (_sessionManager.CurrentProfile?.Stations != null)
        {
            foreach (var station in _sessionManager.CurrentProfile.Stations)
            {
                _atisStationSource.Add(station);
            }
        }

        this.WhenAnyValue(x => x.HasUnsavedChanges).Subscribe(x =>
        {
            if (PresetsViewModel != null)
            {
                PresetsViewModel.HasUnsavedChanges = x;
            }

            RequiresDisconnect = HasUnsavedChanges;
        });

        _atisStationSource.Connect()
            .AutoRefresh(x => x.Name)
            .AutoRefresh(x => x.AtisType)
            .AutoRefresh(x => x.Ordinal)
            .Sort(SortExpressionComparer<AtisStation>
                .Ascending(i => i.Ordinal)
                .ThenBy(i => i.Identifier)
                .ThenBy(i => i.AtisType))
            .Bind(out var sortedStations)
            .Subscribe(_ => { AtisStations = sortedStations; })
            .DisposeWith(_disposables);
        AtisStations = sortedStations;
    }

    /// <summary>
    /// Gets the instance of <see cref="GeneralConfigViewModel"/> used for general configuration settings in the ATIS configuration window.
    /// </summary>
    public GeneralConfigViewModel? GeneralConfigViewModel { get; private set; }

    /// <summary>
    /// Gets the instance of <see cref="PresetsViewModel"/> used to manage presets in the ATIS configuration window.
    /// </summary>
    public PresetsViewModel? PresetsViewModel { get; private set; }

    /// <summary>
    /// Gets the instance of <see cref="FormattingViewModel"/> used for managing ATIS message formatting settings in the ATIS configuration window.
    /// </summary>
    public FormattingViewModel? FormattingViewModel { get; private set; }

    /// <summary>
    /// Gets the instance of <see cref="ContractionsViewModel"/> used for managing contraction-related settings
    /// in the ATIS configuration window.
    /// </summary>
    public ContractionsViewModel? ContractionsViewModel { get; private set; }

    /// <summary>
    /// Gets the instance of <see cref="SandboxViewModel"/> used for managing and applying
    /// sandbox-specific configurations in the ATIS configuration window.
    /// </summary>
    public SandboxViewModel? SandboxViewModel { get; private set; }

    /// <summary>
    /// Gets the command that handles the logic to close a window that implements the <see cref="ICloseable"/> interface.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }

    /// <summary>
    /// Gets the command that executes functionality when the selected ATIS station changes.
    /// </summary>
    public ReactiveCommand<AtisStation, Unit> SelectedAtisStationChanged { get; }

    /// <summary>
    /// Gets the command responsible for saving changes and closing the associated window.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> SaveAndCloseCommand { get; }

    /// <summary>
    /// Gets the command used to apply changes made in the ATIS configuration window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ApplyChangesCommand { get; }

    /// <summary>
    /// Gets the reactive command used to cancel unsaved changes and close the window.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> CancelChangesCommand { get; }

    /// <summary>
    /// Gets the command that opens a dialog for creating a new ATIS station.
    /// </summary>
    public ReactiveCommand<Unit, Unit> NewAtisStationDialogCommand { get; }

    /// <summary>
    /// Gets the command used to export the selected ATIS station configuration.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportAtisCommand { get; }

    /// <summary>
    /// Gets the command that deletes the selected ATIS station.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DeleteAtisCommand { get; }

    /// <summary>
    /// Gets the command used to rename an ATIS station.
    /// </summary>
    public ReactiveCommand<Unit, Unit> RenameAtisCommand { get; }

    /// <summary>
    /// Gets the command responsible for copying an ATIS station in the ATIS configuration.
    /// </summary>
    public ReactiveCommand<Unit, Unit> CopyAtisCommand { get; }

    /// <summary>
    /// Gets the command that handles importing an ATIS station.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ImportAtisStationCommand { get; }

    /// <summary>
    /// Gets the command that opens the dialog to sort ATIS stations.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenSortAtisStationsDialogCommand { get; }

    /// <summary>
    /// Gets or sets the collection of <see cref="AtisStation"/> objects used to represent ATIS stations in the configuration window.
    /// </summary>
    public ReadOnlyObservableCollection<AtisStation> AtisStations { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether there are unsaved changes in the current configuration of <see cref="AtisConfigurationWindowViewModel"/>.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the ATIS needs to be disconnected to apply the changes.
    /// </summary>
    public bool RequiresDisconnect
    {
        get => _requiresDisconnect;
        set => this.RaiseAndSetIfChanged(ref _requiresDisconnect, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog overlay should be displayed in the ATIS configuration window.
    /// </summary>
    public bool ShowOverlay
    {
        get => _showOverlay;
        set => this.RaiseAndSetIfChanged(ref _showOverlay, value);
    }

    /// <summary>
    /// Gets or sets the index of the selected tab in the TabControl within the ATIS configuration window.
    /// </summary>
    public int SelectedTabControlTabIndex
    {
        get => _selectedTabControlTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabControlTabIndex, value);
    }

    /// <summary>
    /// Gets or sets the currently selected instance of <see cref="AtisStation"/> in the ATIS configuration window.
    /// </summary>
    public AtisStation? SelectedAtisStation
    {
        get => _selectedAtisStation;
        set => this.RaiseAndSetIfChanged(ref _selectedAtisStation, value);
    }

    /// <summary>
    /// Initializes the configuration window view model with the specified dialog owner.
    /// </summary>
    /// <param name="dialogOwner">The dialog owner interface instance used for dialog interactions.</param>
    public void Initialize(IDialogOwner dialogOwner)
    {
        _dialogOwner = dialogOwner;

        GeneralConfigViewModel = _viewModelFactory.CreateGeneralConfigViewModel();
        GeneralConfigViewModel.AvailableVoices =
            new ObservableCollection<VoiceMetaData>(_textToSpeechService.VoiceList);
        GeneralConfigViewModel.WhenAnyValue(x => x.SelectedTabIndex).Subscribe(idx =>
        {
            SelectedTabControlTabIndex = idx;
        });
        GeneralConfigViewModel.WhenAnyValue(x => x.HasUnsavedChanges).Subscribe(val => { HasUnsavedChanges = val; });

        PresetsViewModel = _viewModelFactory.CreatePresetsViewModel();
        PresetsViewModel.DialogOwner = _dialogOwner;
        PresetsViewModel.WhenAnyValue(x => x.HasUnsavedChanges).Subscribe(val => { HasUnsavedChanges = val; });

        FormattingViewModel = _viewModelFactory.CreateFormattingViewModel();
        FormattingViewModel.DialogOwner = _dialogOwner;
        FormattingViewModel.WhenAnyValue(x => x.HasUnsavedChanges).Subscribe(val => { HasUnsavedChanges = val; });

        ContractionsViewModel = _viewModelFactory.CreateContractionsViewModel();
        ContractionsViewModel.SetDialogOwner(_dialogOwner);

        SandboxViewModel = _viewModelFactory.CreateSandboxViewModel();
        SandboxViewModel.DialogOwner = _dialogOwner;

        _disposables.Add(GeneralConfigViewModel);
        _disposables.Add(PresetsViewModel);
        _disposables.Add(FormattingViewModel);
        _disposables.Add(ContractionsViewModel);
        _disposables.Add(SandboxViewModel);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged -= OnCollectionChanged;
        }

        GC.SuppressFinalize(this);
    }

    private async Task HandleCloseWindow(ICloseable? window)
    {
        if (_dialogOwner == null)
        {
            window?.Close();
            return;
        }

        NativeAudio.StopBufferPlayback();

        if (SelectedAtisStation != null && HasUnsavedChanges)
        {
            if (await MessageBox.ShowDialog((Window)_dialogOwner,
                    "You have unsaved changes. Are you sure you want to discard them?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
            {
                window?.Close();
            }
        }
        else
        {
            window?.Close();
        }
    }

    private async Task HandleCancelChanges(ICloseable window)
    {
        if (_dialogOwner == null)
        {
            window.Close();
            return;
        }

        NativeAudio.StopBufferPlayback();

        if (SelectedAtisStation != null && HasUnsavedChanges)
        {
            if (await MessageBox.ShowDialog((Window)_dialogOwner,
                    "You have unsaved changes. Are you sure you want to discard them?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
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
        if (SelectedAtisStation == null)
            return;

        if (_dialogOwner == null)
            return;

        if (await MessageBox.ShowDialog((Window)_dialogOwner,
                $"Are you sure you want to delete the selected ATIS Station? This action will also delete all associated ATIS presets.\r\n\r\n{SelectedAtisStation}",
                "Confirm", MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (_sessionManager.CurrentProfile?.Stations == null)
                return;

            EventBus.Instance.Publish(new AtisStationDeleted(SelectedAtisStation.Id));
            _sessionManager.CurrentProfile.Stations.Remove(SelectedAtisStation);
            _profileRepository.Save(_sessionManager.CurrentProfile);
            _atisStationSource.Remove(SelectedAtisStation);
            SelectedAtisStation = null;
            ResetFields();
        }

        ClearAllErrors();
    }

    private async Task HandleCopyAtis()
    {
        if (_dialogOwner == null)
            return;

        if (SelectedAtisStation == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var previousAirportIdentifier = "";
            var previousStationName = "";
            var previousAtisType = AtisType.Combined;

            var dialog = _windowFactory.CreateNewAtisStationDialog();
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
                        else if (_navDataRepository.GetAirport(context.AirportIdentifier) == null)
                        {
                            context.RaiseError("AirportIdentifier", "Airport Identifier does not exist.");
                        }

                        if (string.IsNullOrWhiteSpace(context.StationName))
                        {
                            context.RaiseError("StationName", "Name is required.");
                        }

                        uint parsedFrequency = 0;
                        if (string.IsNullOrEmpty(context.Frequency))
                        {
                            context.RaiseError("Frequency", "Frequency is required.");
                        }
                        else if (!FrequencyValidator.TryParseMHz(context.Frequency, out parsedFrequency, out var error))
                        {
                            if (error != null)
                            {
                                context.RaiseError("Frequency", error);
                            }
                        }

                        if (context.HasErrors) return;

                        if (_atisStationSource.Items.Any(x =>
                                x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType))
                        {
                            context.RaiseError("Duplicate", "");

                            if (await MessageBox.ShowDialog(dialog,
                                    $"{context.AirportIdentifier} ({context.AtisType}) already exists.", "Error",
                                    MessageBoxButton.Ok, MessageBoxIcon.Information) == MessageBoxResult.Ok)
                            {
                                return;
                            }
                        }

                        if (_sessionManager.CurrentProfile?.Stations != null)
                        {
                            var clone = SelectedAtisStation.Clone();
                            clone.Identifier = context.AirportIdentifier;
                            clone.Name = context.StationName;
                            clone.AtisType = context.AtisType;
                            clone.Frequency = parsedFrequency;

                            _sessionManager.CurrentProfile.Stations.Add(clone);
                            _profileRepository.Save(_sessionManager.CurrentProfile);
                            _atisStationSource.Add(clone);
                            SelectedAtisStation = clone;
                            EventBus.Instance.Publish(new AtisStationAdded(SelectedAtisStation.Id));
                        }
                    }
                };
                await dialog.ShowDialog((Window)_dialogOwner);
            }
        }
    }

    private async Task HandleOpenSortAtisStationsDialog()
    {
        try
        {
            if (_dialogOwner == null)
                return;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            {
                if (lifetime.MainWindow == null)
                {
                    return;
                }

                var dialog = _windowFactory.CreateSortAtisStationsDialog();
                dialog.Topmost = lifetime.MainWindow.Topmost;
                if (dialog.DataContext is SortAtisStationsDialogViewModel context)
                {
                    if (_sessionManager.CurrentProfile == null)
                    {
                        Log.Error("Current profile is null");
                        return;
                    }

                    if (_sessionManager.CurrentProfile.Stations == null)
                    {
                        Log.Error("Current profile stations is null");
                        return;
                    }

                    context.AtisStations = new ObservableCollection<AtisStation>(_atisStationSource.Items);
                    context.AtisStations.CollectionChanged += (_, _) =>
                    {
                        try
                        {
                            var idx = 0;
                            var updatedStations = new List<AtisStation>();
                            foreach (var item in context.AtisStations)
                            {
                                item.Ordinal = ++idx;
                                updatedStations.Add(item);
                            }

                            // Validate ordinals
                            var ordinals = updatedStations.Select(x => x.Ordinal).ToList();
                            if (ordinals.Count != ordinals.Distinct().Count())
                            {
                                Log.Error("Duplicate ordinals detected");
                                return;
                            }

                            // Update source and publish events
                            _atisStationSource.Clear();
                            foreach (var item in updatedStations)
                            {
                                _atisStationSource.Add(item);
                                EventBus.Instance.Publish(new AtisStationOrdinalChanged(item.Id, item.Ordinal));
                            }

                            // Save changes
                            _sessionManager.CurrentProfile.Stations = updatedStations;
                            _profileRepository.Save(_sessionManager.CurrentProfile);

                            ResetFields();
                            SelectedAtisStation = null;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to update station ordinals");

                            // Restore original order
                            context.AtisStations = new ObservableCollection<AtisStation>(_atisStationSource.Items);
                        }
                    };
                    await dialog.ShowDialog((Window)_dialogOwner);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in HandleSortAtisStations");
        }
    }

    private async void HandleImportAtisStation()
    {
        try
        {
            if (_dialogOwner == null)
                return;

            if (_sessionManager.CurrentProfile == null)
                return;

            var filters = new List<FilePickerFileType> { new("ATIS Station (*.station)") { Patterns = ["*.station"] } };
            var files = await FilePickerExtensions.OpenFilePickerAsync(filters, "Import ATIS Station");

            if (files == null)
                return;

            foreach (var file in files)
            {
                var fileContent = await File.ReadAllTextAsync(file);
                var station = JsonSerializer.Deserialize(fileContent, SourceGenerationContext.NewDefault.AtisStation);
                if (station != null)
                {
                    if (_sessionManager.CurrentProfile?.Stations == null)
                        return;

                    if (_sessionManager.CurrentProfile.Stations.Any(x =>
                            x.Identifier == station.Identifier && x.AtisType == station.AtisType))
                    {
                        if (await MessageBox.ShowDialog((Window)_dialogOwner,
                                $"{station.Identifier} ({station.AtisType}) already exists. Do you want to overwrite it?",
                                "Confirm",
                                MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
                        {
                            _sessionManager.CurrentProfile.Stations.RemoveAll(x =>
                                x.Identifier == station.Identifier && x.AtisType == station.AtisType);
                        }
                        else
                        {
                            return;
                        }
                    }

                    _sessionManager.CurrentProfile.Stations.Add(station);
                    _profileRepository.Save(_sessionManager.CurrentProfile);
                    _atisStationSource.Add(station);
                    EventBus.Instance.Publish(new AtisStationAdded(station.Id));
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
        if (SelectedAtisStation == null)
            return;

        if (_sessionManager.CurrentProfile == null)
            return;

        if (_dialogOwner == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var previousValue = SelectedAtisStation.Name;

            var dialog = _windowFactory.CreateUserInputDialog();
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
                            var existing = _atisStationSource.Items.FirstOrDefault(x => x == SelectedAtisStation);
                            if (existing != null)
                            {
                                existing.Name = context.UserValue.Trim();
                                _profileRepository.Save(_sessionManager.CurrentProfile);
                                ResetFields();
                                SelectedAtisStation = null;
                            }
                        }
                    }
                };
            }

            await dialog.ShowDialog((Window)_dialogOwner);
        }
    }

    private async Task HandleExportAtisStation()
    {
        if (SelectedAtisStation == null)
            return;

        if (_dialogOwner == null)
            return;

        var filters = new List<FilePickerFileType> { new("ATIS Station (*.station)") { Patterns = ["*.station"] } };
        var file = await FilePickerExtensions.SaveFileAsync("Export ATIS Station", filters,
            $"{SelectedAtisStation.Name} ({SelectedAtisStation.AtisType})");

        if (file == null)
            return;

        SelectedAtisStation.Id = null!;

        await File.WriteAllTextAsync(file.Path.LocalPath,
            JsonSerializer.Serialize(SelectedAtisStation, SourceGenerationContext.NewDefault.AtisStation));
        await MessageBox.ShowDialog((Window)_dialogOwner, "ATIS Station successfully exported.", "Success",
            MessageBoxButton.Ok, MessageBoxIcon.Information);
    }

    private async Task HandleNewAtisStationDialog()
    {
        if (_dialogOwner == null)
            return;

        if (_sessionManager.CurrentProfile == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var previousAirportIdentifier = "";
            var previousStationName = "";
            var previousAtisType = AtisType.Combined;
            var isDuplicateAcknowledged = false;

            var dialog = _windowFactory.CreateNewAtisStationDialog();
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
                        else if (_navDataRepository.GetAirport(context.AirportIdentifier) == null)
                        {
                            context.RaiseError("AirportIdentifier", "Airport Identifier does not exist.");
                        }

                        if (string.IsNullOrWhiteSpace(context.StationName))
                        {
                            context.RaiseError("StationName", "Name is required.");
                        }

                        uint parsedFrequency = 0;
                        if (string.IsNullOrEmpty(context.Frequency))
                        {
                            context.RaiseError("Frequency", "Frequency is required.");
                        }
                        else if (!FrequencyValidator.TryParseMHz(context.Frequency, out parsedFrequency, out var error))
                        {
                            if (error != null)
                            {
                                context.RaiseError("Frequency", error);
                            }
                        }

                        if (context.HasErrors) return;

                        if (_atisStationSource.Items.Any(x =>
                                x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType))
                        {
                            if (!isDuplicateAcknowledged)
                            {
                                context.RaiseError("Duplicate", "");
                            }

                            if (await MessageBox.ShowDialog(dialog,
                                    $"{context.AirportIdentifier} ({context.AtisType}) already exists. Would you like to overwrite it?",
                                    "Error", MessageBoxButton.YesNo, MessageBoxIcon.Information) ==
                                MessageBoxResult.Yes)
                            {
                                isDuplicateAcknowledged = true;

                                context.ClearErrors("Duplicate");
                                context.OkButtonCommand.Execute(dialog).Subscribe();

                                var existing = _sessionManager.CurrentProfile.Stations?.FirstOrDefault(x =>
                                    x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType);
                                if (existing != null)
                                {
                                    _atisStationSource.Remove(existing);
                                    _sessionManager.CurrentProfile.Stations?.Remove(existing);
                                    EventBus.Instance.Publish(new AtisStationDeleted(existing.Id));
                                }
                            }
                            else
                            {
                                return;
                            }
                        }

                        var station = new AtisStation()
                        {
                            Identifier = context.AirportIdentifier,
                            Name = context.StationName,
                            Frequency = parsedFrequency,
                            AtisType = context.AtisType
                        };
                        _sessionManager.CurrentProfile.Stations?.Add(station);
                        _profileRepository.Save(_sessionManager.CurrentProfile);

                        _atisStationSource.Add(station);
                        SelectedAtisStation = station;

                        EventBus.Instance.Publish(new AtisStationAdded(station.Id));
                    }
                };
                await dialog.ShowDialog((Window)_dialogOwner);
            }
        }
    }

    private void HandleSaveAndClose(ICloseable? window)
    {
        if (SelectedAtisStation == null)
        {
            window?.Close();
            return;
        }

        var general = GeneralConfigViewModel?.ApplyConfig();
        var presets = PresetsViewModel?.ApplyConfig();
        var formatting = FormattingViewModel?.ApplyChanges();

        if ((general.HasValue && general.Value) || (presets.HasValue && presets.Value) ||
            (formatting.HasValue && formatting.Value))
        {
            EventBus.Instance.Publish(new AtisStationUpdated(SelectedAtisStation.Id));
        }

        window?.Close();
    }

    private void HandleApplyChanges()
    {
        GeneralConfigViewModel?.ApplyConfig();
        PresetsViewModel?.ApplyConfig();
        FormattingViewModel?.ApplyChanges();
        SandboxViewModel?.ApplyConfig();
        RequiresDisconnect = true;
    }

    private void HandleSelectedAtisStationChanged(AtisStation? station)
    {
        if (station == null)
            return;

        SelectedAtisStation = station;

        GeneralConfigViewModel?.AtisStationChanged.Execute(station).Subscribe();
        PresetsViewModel?.AtisStationChanged.Execute(station).Subscribe();
        FormattingViewModel?.AtisStationChanged.Execute(station).Subscribe();
        ContractionsViewModel?.AtisStationChanged.Execute(station).Subscribe();
        SandboxViewModel?.AtisStationChanged.Execute(station).Subscribe();
    }

    private void ResetFields()
    {
        GeneralConfigViewModel?.Reset();
        SelectedTabControlTabIndex = 0;
        HasUnsavedChanges = false;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ShowOverlay = lifetime.Windows.Count(w => w.GetType() != typeof(Windows.MainWindow)) > 1;
        }
    }
}
