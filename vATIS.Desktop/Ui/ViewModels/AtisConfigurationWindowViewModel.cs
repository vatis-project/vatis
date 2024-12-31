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
using Vatsim.Vatis.Utils;
using Vatsim.Vatis.Voice.Audio;

namespace Vatsim.Vatis.Ui.ViewModels;

public class AtisConfigurationWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable mDisposables = new();
    private readonly IAppConfig mAppConfig;
    private readonly ISessionManager mSessionManager;
    private readonly IWindowFactory mWindowFactory;
    private readonly INavDataRepository mNavDataRepository;
    private readonly IViewModelFactory mViewModelFactory;
    private readonly ITextToSpeechService mTextToSpeechService;
    private readonly IProfileRepository mProfileRepository;
    private IDialogOwner? mDialogOwner;
    
    public GeneralConfigViewModel? GeneralConfigViewModel { get; private set; }
    public PresetsViewModel? PresetsViewModel { get;  private set;}
    public FormattingViewModel? FormattingViewModel { get; private set; }
    public ContractionsViewModel? ContractionsViewModel { get; private set; }
    public SandboxViewModel? SandboxViewModel { get; private set; }

    public ReactiveCommand<ICloseable, Unit> CloseWindowCommand { get; }
    public ReactiveCommand<AtisStation, Unit> SelectedAtisStationChanged { get;  }
    public ReactiveCommand<ICloseable, Unit> SaveAndCloseCommand { get;  }
    public ReactiveCommand<Unit, Unit> ApplyChangesCommand { get; }
    public ReactiveCommand<ICloseable, Unit> CancelChangesCommand { get;  }
    public ReactiveCommand<Unit, Unit> NewAtisStationDialogCommand { get;  }
    public ReactiveCommand<Unit, Unit> ExportAtisCommand { get;  }
    public ReactiveCommand<Unit, Unit> DeleteAtisCommand { get;  }
    public ReactiveCommand<Unit, Unit> RenameAtisCommand { get;  }
    public ReactiveCommand<Unit, Unit> CopyAtisCommand { get;  }
    public ReactiveCommand<Unit, Unit> ImportAtisStationCommand { get;  }

    #region Reactive UI Properties
    private readonly SourceList<AtisStation> mAtisStationSource = new();
    public ReadOnlyObservableCollection<AtisStation> AtisStations { get; set; }
    
    private bool mHasUnsavedChanges;
    public bool HasUnsavedChanges
    {
        get => mHasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref mHasUnsavedChanges, value);
    }

    private bool mShowOverlay;
    public bool ShowOverlay
    {
        get => mShowOverlay;
        set => this.RaiseAndSetIfChanged(ref mShowOverlay, value);
    }

    private int mSelectedTabControlTabIndex;
    public int SelectedTabControlTabIndex
    {
        get => mSelectedTabControlTabIndex;
        set => this.RaiseAndSetIfChanged(ref mSelectedTabControlTabIndex, value);
    }

    private AtisStation? mSelectedAtisStation;
    public AtisStation? SelectedAtisStation
    {
        get => mSelectedAtisStation;
        set => this.RaiseAndSetIfChanged(ref mSelectedAtisStation, value);
    }
    #endregion

    public AtisConfigurationWindowViewModel(IAppConfig appConfig,
        ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IViewModelFactory viewModelFactory,
        ITextToSpeechService textToSpeechService,
        INavDataRepository navDataRepository,
        IProfileRepository profileRepository)
    {
        mAppConfig = appConfig;
        mSessionManager = sessionManager;
        mWindowFactory = windowFactory;
        mViewModelFactory = viewModelFactory;
        mTextToSpeechService = textToSpeechService;
        mNavDataRepository = navDataRepository;
        mProfileRepository = profileRepository;
        
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
        
        mDisposables.Add(CloseWindowCommand);
        mDisposables.Add(SaveAndCloseCommand);
        mDisposables.Add(ApplyChangesCommand);
        mDisposables.Add(CancelChangesCommand);
        mDisposables.Add(NewAtisStationDialogCommand);
        mDisposables.Add(ExportAtisCommand);
        mDisposables.Add(DeleteAtisCommand);
        mDisposables.Add(RenameAtisCommand);
        mDisposables.Add(CopyAtisCommand);
        mDisposables.Add(ImportAtisStationCommand);
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                ShowOverlay = lifetime.Windows.Count(w => w.GetType() != typeof(Windows.MainWindow)) > 1;
            };
        }

        if (mSessionManager.CurrentProfile?.Stations != null)
        {
            foreach (var station in mSessionManager.CurrentProfile.Stations)
            {
                mAtisStationSource.Add(station);
            }
        }

        mAtisStationSource.Connect()
            .AutoRefresh(x => x.Name)
            .AutoRefresh(x => x.AtisType)
            .Sort(SortExpressionComparer<AtisStation>
                .Ascending(i => i.Identifier)
                .ThenBy(i => i.AtisType))
            .Bind(out var sortedStations)
            .Subscribe(_ =>
            {
                AtisStations = sortedStations;
            });
        AtisStations = sortedStations;
    }

    private async Task HandleCloseWindow(ICloseable? window)
    {
        if (mDialogOwner == null)
        {
            window?.Close();
            return;
        }

        NativeAudio.StopBufferPlayback();

        if (SelectedAtisStation != null && HasUnsavedChanges)
        {
            if (await MessageBox.ShowDialog((Window)mDialogOwner,
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

    public void Initialize(IDialogOwner dialogOwner)
    {
        mDialogOwner = dialogOwner;
        
        GeneralConfigViewModel = mViewModelFactory.CreateGeneralConfigViewModel();
        GeneralConfigViewModel.AvailableVoices = new ObservableCollection<VoiceMetaData>(mTextToSpeechService.VoiceList);
        GeneralConfigViewModel.WhenAnyValue(x => x.SelectedTabIndex).Subscribe(idx =>
        {
            SelectedTabControlTabIndex = idx;
        });
        GeneralConfigViewModel.WhenAnyValue(x => x.HasUnsavedChanges).Subscribe(val =>
        {
            HasUnsavedChanges = val;
        });
        
        PresetsViewModel = mViewModelFactory.CreatePresetsViewModel();
        PresetsViewModel.DialogOwner = mDialogOwner;
        PresetsViewModel.WhenAnyValue(x => x.HasUnsavedChanges).Subscribe(val =>
        {
            HasUnsavedChanges = val;
        });

        FormattingViewModel = mViewModelFactory.CreateFormattingViewModel();
        FormattingViewModel.DialogOwner = mDialogOwner;
        FormattingViewModel.WhenAnyValue(x => x.HasUnsavedChanges).Subscribe(val =>
        {
            HasUnsavedChanges = val;
        });

        ContractionsViewModel = mViewModelFactory.CreateContractionsViewModel();
        ContractionsViewModel.SetDialogOwner(mDialogOwner);
        
        SandboxViewModel = mViewModelFactory.CreateSandboxViewModel();
        SandboxViewModel.DialogOwner = mDialogOwner;
    }

    private async Task HandleCancelChanges(ICloseable window)
    {
        if (mDialogOwner == null)
        {
            window.Close();
            return;
        }

        NativeAudio.StopBufferPlayback();

        if (SelectedAtisStation != null && HasUnsavedChanges)
        {
            if (await MessageBox.ShowDialog((Window)mDialogOwner,
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

        if (mDialogOwner == null)
            return;
        
        if (await MessageBox.ShowDialog((Window)mDialogOwner, $"Are you sure you want to delete the selected ATIS Station? This action will also delete all associated ATIS presets.\r\n\r\n{SelectedAtisStation}", "Confirm", MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
        {
            if (mSessionManager.CurrentProfile?.Stations == null)
                return;

            MessageBus.Current.SendMessage(new AtisStationDeleted(SelectedAtisStation.Id));
            mSessionManager.CurrentProfile.Stations.Remove(SelectedAtisStation);
            mAppConfig.SaveConfig();
            mAtisStationSource.Remove(SelectedAtisStation);
            SelectedAtisStation = null;
            ResetFields();
        }
        ClearAllErrors();
    }

    private async Task HandleCopyAtis()
    {
        if (mDialogOwner == null)
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

            var dialog = mWindowFactory.CreateNewAtisStationDialog();
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
                        else if (mNavDataRepository.GetAirport(context.AirportIdentifier) == null)
                        {
                            context.RaiseError("AirportIdentifier", "Airport Identifier does not exist.");
                        }

                        if (string.IsNullOrWhiteSpace(context.StationName))
                        {
                            context.RaiseError("StationName", "Name is required.");
                        }

                        if (context.HasErrors) return;

                        if (mAtisStationSource.Items.Any(x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType))
                        {
                            context.RaiseError("Duplicate", "");
                            
                            if (await MessageBox.ShowDialog(dialog, $"{context.AirportIdentifier} ({context.AtisType}) already exists.", "Error", MessageBoxButton.Ok, MessageBoxIcon.Information) == MessageBoxResult.Ok)
                            {
                                return;
                            }
                        }

                        if (mSessionManager.CurrentProfile?.Stations != null)
                        {
                            var clone = SelectedAtisStation.Clone();
                            clone.Identifier = context.AirportIdentifier;
                            clone.Name = context.StationName;
                            clone.AtisType = context.AtisType;

                            mSessionManager.CurrentProfile.Stations.Add(clone);
                            mAppConfig.SaveConfig();
                            mAtisStationSource.Add(clone);
                            SelectedAtisStation = clone;
                            MessageBus.Current.SendMessage(new AtisStationAdded(SelectedAtisStation.Id));
                        }
                    }
                };
                await dialog.ShowDialog((Window)mDialogOwner);
            }
        }
    }
    
    private async void HandleImportAtisStation()
    {
        try
        {
            if (mDialogOwner == null)
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
                    if (mSessionManager.CurrentProfile?.Stations == null)
                        return;

                    if (mSessionManager.CurrentProfile.Stations.Any(x =>
                            x.Identifier == station.Identifier && x.AtisType == station.AtisType))
                    {
                        if (await MessageBox.ShowDialog((Window)mDialogOwner,
                                $"{station.Identifier} ({station.AtisType}) already exists. Do you want to overwrite it?",
                                "Confirm",
                                MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
                        {
                            mSessionManager.CurrentProfile?.Stations?.RemoveAll(x =>
                                x.Identifier == station.Identifier && x.AtisType == station.AtisType);
                        }
                        else
                        {
                            return;
                        }
                    }

                    mSessionManager.CurrentProfile?.Stations?.Add(station);
                    mAppConfig.SaveConfig();
                    mAtisStationSource.Add(station);
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
        if (SelectedAtisStation == null)
            return;

        if (mDialogOwner == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var previousValue = SelectedAtisStation.Name;
            
            var dialog = mWindowFactory.CreateUserInputDialog();
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
                            SelectedAtisStation.Name = context.UserValue.Trim();
                            mAppConfig.SaveConfig();
                            mAtisStationSource.Edit(list =>
                            {
                                var item = list.FirstOrDefault(x => x.Id == SelectedAtisStation.Id);
                                if (item != null)
                                {
                                    item.Name = context.UserValue.Trim();
                                }
                            });
                        }
                    }
                };
            }
            await dialog.ShowDialog((Window)mDialogOwner);
        }
    }

    private async Task HandleExportAtisStation()
    {
        if (SelectedAtisStation == null)
            return;

        if (mDialogOwner == null)
            return;

        var filters = new List<FilePickerFileType> { new("ATIS Station (*.station)") { Patterns = ["*.station"] } };
        var file = await FilePickerExtensions.SaveFileAsync("Export ATIS Station", filters, $"{SelectedAtisStation.Name} ({SelectedAtisStation.AtisType})");

        if (file == null) 
            return;

        SelectedAtisStation.Id = null!;

        await File.WriteAllTextAsync(file.Path.LocalPath, JsonSerializer.Serialize(SelectedAtisStation, SourceGenerationContext.NewDefault.AtisStation));
        await MessageBox.ShowDialog((Window)mDialogOwner, "ATIS Station successfully exported.", "Success", MessageBoxButton.Ok, MessageBoxIcon.Information);
    }

    private async Task HandleNewAtisStationDialog()
    {
        if (mDialogOwner == null)
            return;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;
            
            var previousAirportIdentifier = "";
            var previousStationName = "";
            var previousAtisType = AtisType.Combined;
            var isDuplicateAcknowledged = false;

            var dialog = mWindowFactory.CreateNewAtisStationDialog();
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
                        else if (mNavDataRepository.GetAirport(context.AirportIdentifier) == null)
                        {
                            context.RaiseError("AirportIdentifier", "Airport Identifier does not exist.");
                        }

                        if (string.IsNullOrWhiteSpace(context.StationName))
                        {
                            context.RaiseError("StationName", "Name is required.");
                        }

                        if (context.HasErrors) return;

                        if (mAtisStationSource.Items.Any(x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType))
                        {
                            if (!isDuplicateAcknowledged)
                            {
                                context.RaiseError("Duplicate", "");
                            }

                            if (await MessageBox.ShowDialog(dialog, $"{context.AirportIdentifier} ({context.AtisType}) already exists. Would you like to overwrite it?", "Error", MessageBoxButton.YesNo, MessageBoxIcon.Information) == MessageBoxResult.Yes)
                            {
                                isDuplicateAcknowledged = true;

                                context.ClearErrors("Duplicate");
                                context.OkButtonCommand.Execute(dialog).Subscribe();

                                mSessionManager.CurrentProfile?.Stations?.RemoveAll(x => x.Identifier == context.AirportIdentifier && x.AtisType == context.AtisType);
                                mAppConfig.SaveConfig();
                            }
                            else
                            {
                                return;
                            }
                        }

                        if (mSessionManager.CurrentProfile?.Stations != null)
                        {
                            var station = new AtisStation()
                            {
                                Identifier = context.AirportIdentifier,
                                Name = context.StationName,
                                AtisType = context.AtisType
                            };
                            mSessionManager.CurrentProfile.Stations.Add(station);
                            mProfileRepository.Save(mSessionManager.CurrentProfile);
                            
                            mAtisStationSource.Add(station);
                            SelectedAtisStation = station;
                            
                            MessageBus.Current.SendMessage(new AtisStationAdded(station.Id));
                        }
                    }
                };
                await dialog.ShowDialog((Window)mDialogOwner);
            }
        }
    }

    private void HandleSaveAndClose(ICloseable? window)
    {
        var general = GeneralConfigViewModel?.ApplyConfig();
        var presets = PresetsViewModel?.ApplyConfig();
        var formatting = FormattingViewModel?.ApplyChanges();
        var sandbox = SandboxViewModel?.ApplyConfig();

        if (general != null && presets != null && formatting != null && sandbox != null)
        {
            if (general.Value && presets.Value && formatting.Value && sandbox.Value)
            {
                if (SelectedAtisStation != null)
                {
                    MessageBus.Current.SendMessage(new AtisStationUpdated(SelectedAtisStation.Id));
                }
                window?.Close();
            }
        }
    }

    private void HandleApplyChanges()
    {
        GeneralConfigViewModel?.ApplyConfig();
        PresetsViewModel?.ApplyConfig();
        FormattingViewModel?.ApplyChanges();
        SandboxViewModel?.ApplyConfig();
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

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        mDisposables.Dispose();
    }
}