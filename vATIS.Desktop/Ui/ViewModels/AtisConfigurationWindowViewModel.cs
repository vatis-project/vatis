// <copyright file="AtisConfigurationWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <summary>
/// Represents the view model for the ATIS configuration window.
/// </summary>
public class AtisConfigurationWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAppConfig appConfig;
    private readonly CompositeDisposable disposables = new();
    private readonly INavDataRepository navDataRepository;
    private readonly IProfileRepository profileRepository;
    private readonly ISessionManager sessionManager;
    private readonly ITextToSpeechService textToSpeechService;
    private readonly IViewModelFactory viewModelFactory;
    private readonly IWindowFactory windowFactory;
    private readonly SourceList<AtisStation> atisStationSource = new();
    private IDialogOwner? dialogOwner;
    private bool hasUnsavedChanges;
    private bool showOverlay;
    private int selectedTabControlTabIndex;
    private AtisStation? selectedAtisStation;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisConfigurationWindowViewModel"/> class.
    /// </summary>
    /// <param name="appConfig">The application configuration data.</param>
    /// <param name="sessionManager">The session manager instance.</param>
    /// <param name="windowFactory">The factory used to create application windows.</param>
    /// <param name="viewModelFactory">The factory used to create view models.</param>
    /// <param name="textToSpeechService">The text-to-speech service for the application.</param>
    /// <param name="navDataRepository">The navigation data repository.</param>
    /// <param name="profileRepository">The profile repository for managing user settings and profiles.</param>
    public AtisConfigurationWindowViewModel(
        IAppConfig appConfig,
        ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IViewModelFactory viewModelFactory,
        ITextToSpeechService textToSpeechService,
        INavDataRepository navDataRepository,
        IProfileRepository profileRepository)
    {
        this.appConfig = appConfig;
        this.sessionManager = sessionManager;
        this.windowFactory = windowFactory;
        this.viewModelFactory = viewModelFactory;
        this.textToSpeechService = textToSpeechService;
        this.navDataRepository = navDataRepository;
        this.profileRepository = profileRepository;

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

        this.disposables.Add(this.CloseWindowCommand);
        this.disposables.Add(this.SaveAndCloseCommand);
        this.disposables.Add(this.ApplyChangesCommand);
        this.disposables.Add(this.CancelChangesCommand);
        this.disposables.Add(this.NewAtisStationDialogCommand);
        this.disposables.Add(this.ExportAtisCommand);
        this.disposables.Add(this.DeleteAtisCommand);
        this.disposables.Add(this.RenameAtisCommand);
        this.disposables.Add(this.CopyAtisCommand);
        this.disposables.Add(this.ImportAtisStationCommand);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                this.ShowOverlay = lifetime.Windows.Count(w => w.GetType() != typeof(MainWindow)) > 1;
            };
        }

        if (this.sessionManager.CurrentProfile?.Stations != null)
        {
            foreach (var station in this.sessionManager.CurrentProfile.Stations)
            {
                this.atisStationSource.Add(station);
            }
        }

        this.atisStationSource.Connect()
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
    /// Gets or sets the collection of <see cref="AtisStation"/> objects used to represent ATIS stations in the configuration window.
    /// </summary>
    public ReadOnlyObservableCollection<AtisStation> AtisStations { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are unsaved changes in the current configuration of <see cref="AtisConfigurationWindowViewModel"/>.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => this.hasUnsavedChanges;
        private set => this.RaiseAndSetIfChanged(ref this.hasUnsavedChanges, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog overlay should be displayed in the ATIS configuration window.
    /// </summary>
    public bool ShowOverlay
    {
        get => this.showOverlay;
        set => this.RaiseAndSetIfChanged(ref this.showOverlay, value);
    }

    /// <summary>
    /// Gets or sets the index of the selected tab in the TabControl within the ATIS configuration window.
    /// </summary>
    public int SelectedTabControlTabIndex
    {
        get => this.selectedTabControlTabIndex;
        set => this.RaiseAndSetIfChanged(ref this.selectedTabControlTabIndex, value);
    }

    /// <summary>
    /// Gets or sets the currently selected instance of <see cref="AtisStation"/> in the ATIS configuration window.
    /// </summary>
    public AtisStation? SelectedAtisStation
    {
        get => this.selectedAtisStation;
        set => this.RaiseAndSetIfChanged(ref this.selectedAtisStation, value);
    }

    /// <summary>
    /// Initializes the configuration window view model with the specified dialog owner.
    /// </summary>
    /// <param name="owner">The dialog owner interface instance used for dialog interactions.</param>
    public void Initialize(IDialogOwner owner)
    {
        this.dialogOwner = owner;

        this.GeneralConfigViewModel = this.viewModelFactory.CreateGeneralConfigViewModel();
        this.GeneralConfigViewModel.AvailableVoices =
            new ObservableCollection<VoiceMetaData>(this.textToSpeechService.VoiceList);
        this.GeneralConfigViewModel.WhenAnyValue(x => x.SelectedTabIndex)
            .Subscribe(idx => { this.SelectedTabControlTabIndex = idx; });
        this.GeneralConfigViewModel.WhenAnyValue(x => x.HasUnsavedChanges)
            .Subscribe(val => { this.HasUnsavedChanges = val; });

        this.PresetsViewModel = this.viewModelFactory.CreatePresetsViewModel();
        this.PresetsViewModel.DialogOwner = this.dialogOwner;
        this.PresetsViewModel.WhenAnyValue(x => x.HasUnsavedChanges)
            .Subscribe(val => { this.HasUnsavedChanges = val; });

        this.FormattingViewModel = this.viewModelFactory.CreateFormattingViewModel();
        this.FormattingViewModel.DialogOwner = this.dialogOwner;
        this.FormattingViewModel.WhenAnyValue(x => x.HasUnsavedChanges)
            .Subscribe(val => { this.HasUnsavedChanges = val; });

        this.ContractionsViewModel = this.viewModelFactory.CreateContractionsViewModel();
        this.ContractionsViewModel.SetDialogOwner(this.dialogOwner);

        this.SandboxViewModel = this.viewModelFactory.CreateSandboxViewModel();
        this.SandboxViewModel.DialogOwner = this.dialogOwner;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.disposables.Dispose();
    }

    private async Task HandleCloseWindow(ICloseable? window)
    {
        if (this.dialogOwner == null)
        {
            window?.Close();
            return;
        }

        NativeAudio.StopBufferPlayback();

        if (this.SelectedAtisStation != null && this.HasUnsavedChanges)
        {
            if (await MessageBox.ShowDialog(
                    (Window)this.dialogOwner,
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

    private async Task HandleCancelChanges(ICloseable window)
    {
        if (this.dialogOwner == null)
        {
            window.Close();
            return;
        }

        NativeAudio.StopBufferPlayback();

        if (this.SelectedAtisStation != null && this.HasUnsavedChanges)
        {
            if (await MessageBox.ShowDialog(
                    (Window)this.dialogOwner,
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

        if (this.dialogOwner == null)
        {
            return;
        }

        if (await MessageBox.ShowDialog(
                (Window)this.dialogOwner,
                $"Are you sure you want to delete the selected ATIS Station? This action will also delete all associated ATIS presets.\r\n\r\n{this.SelectedAtisStation}",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (this.sessionManager.CurrentProfile?.Stations == null)
            {
                return;
            }

            MessageBus.Current.SendMessage(new AtisStationDeleted(this.SelectedAtisStation.Id));
            this.sessionManager.CurrentProfile.Stations.Remove(this.SelectedAtisStation);
            this.appConfig.SaveConfig();
            this.atisStationSource.Remove(this.SelectedAtisStation);
            this.SelectedAtisStation = null;
            this.ResetFields();
        }

        this.ClearAllErrors();
    }

    private async Task HandleCopyAtis()
    {
        if (this.dialogOwner == null)
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

            var previousAirportIdentifier = string.Empty;
            var previousStationName = string.Empty;
            var previousAtisType = AtisType.Combined;

            var dialog = this.windowFactory.CreateNewAtisStationDialog();
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
                        else if (this.navDataRepository.GetAirport(context.AirportIdentifier) == null)
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

                        if (this.atisStationSource.Items.Any(
                                x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType))
                        {
                            context.RaiseError("Duplicate", string.Empty);

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

                        if (this.sessionManager.CurrentProfile?.Stations != null)
                        {
                            var clone = this.SelectedAtisStation.Clone();
                            clone.Identifier = context.AirportIdentifier;
                            clone.Name = context.StationName;
                            clone.AtisType = context.AtisType;

                            this.sessionManager.CurrentProfile.Stations.Add(clone);
                            this.appConfig.SaveConfig();
                            this.atisStationSource.Add(clone);
                            this.SelectedAtisStation = clone;
                            MessageBus.Current.SendMessage(new AtisStationAdded(this.SelectedAtisStation.Id));
                        }
                    }
                };
                await dialog.ShowDialog((Window)this.dialogOwner);
            }
        }
    }

    private async void HandleImportAtisStation()
    {
        try
        {
            if (this.dialogOwner == null)
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
                    if (this.sessionManager.CurrentProfile?.Stations == null)
                    {
                        return;
                    }

                    if (this.sessionManager.CurrentProfile.Stations.Any(
                            x =>
                                x.Identifier == station.Identifier && x.AtisType == station.AtisType))
                    {
                        if (await MessageBox.ShowDialog(
                                (Window)this.dialogOwner,
                                $"{station.Identifier} ({station.AtisType}) already exists. Do you want to overwrite it?",
                                "Confirm",
                                MessageBoxButton.YesNo,
                                MessageBoxIcon.Information) == MessageBoxResult.Yes)
                        {
                            this.sessionManager.CurrentProfile?.Stations?.RemoveAll(
                                x =>
                                    x.Identifier == station.Identifier && x.AtisType == station.AtisType);
                        }
                        else
                        {
                            return;
                        }
                    }

                    this.sessionManager.CurrentProfile?.Stations?.Add(station);
                    this.appConfig.SaveConfig();
                    this.atisStationSource.Add(station);
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

        if (this.dialogOwner == null)
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

            var dialog = this.windowFactory.CreateUserInputDialog();
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
                            this.appConfig.SaveConfig();
                            this.atisStationSource.Edit(
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

            await dialog.ShowDialog((Window)this.dialogOwner);
        }
    }

    private async Task HandleExportAtisStation()
    {
        if (this.SelectedAtisStation == null)
        {
            return;
        }

        if (this.dialogOwner == null)
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
            (Window)this.dialogOwner,
            "ATIS Station successfully exported.",
            "Success",
            MessageBoxButton.Ok,
            MessageBoxIcon.Information);
    }

    private async Task HandleNewAtisStationDialog()
    {
        if (this.dialogOwner == null)
        {
            return;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var previousAirportIdentifier = string.Empty;
            var previousStationName = string.Empty;
            var previousAtisType = AtisType.Combined;
            var isDuplicateAcknowledged = false;

            var dialog = this.windowFactory.CreateNewAtisStationDialog();
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
                        else if (this.navDataRepository.GetAirport(context.AirportIdentifier) == null)
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

                        if (this.atisStationSource.Items.Any(
                                x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType))
                        {
                            if (!isDuplicateAcknowledged)
                            {
                                context.RaiseError("Duplicate", string.Empty);
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

                                this.sessionManager.CurrentProfile?.Stations?.RemoveAll(
                                    x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType);
                                this.appConfig.SaveConfig();
                            }
                            else
                            {
                                return;
                            }
                        }

                        if (this.sessionManager.CurrentProfile?.Stations != null)
                        {
                            var station = new AtisStation
                            {
                                Identifier = context.AirportIdentifier,
                                Name = context.StationName,
                                AtisType = context.AtisType,
                            };
                            this.sessionManager.CurrentProfile.Stations.Add(station);
                            this.profileRepository.Save(this.sessionManager.CurrentProfile);

                            this.atisStationSource.Add(station);
                            this.SelectedAtisStation = station;

                            MessageBus.Current.SendMessage(new AtisStationAdded(station.Id));
                        }
                    }
                };
                await dialog.ShowDialog((Window)this.dialogOwner);
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
}
