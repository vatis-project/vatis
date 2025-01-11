using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Networking.AtisHub.Dto;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;

namespace Vatsim.Vatis.Ui.ViewModels;

public class MainWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IWindowLocationService _windowLocationService;
    private readonly IAtisHubConnection _atisHubConnection;
    private readonly IWebsocketService _websocketService;
    private readonly SourceList<AtisStationViewModel> _atisStationSource = new();
    private string? _beforeStationSortSelectedStationId;

    public Window? Owner { get; set; }
    public ReadOnlyObservableCollection<AtisStationViewModel> AtisStations { get; set; }
    private ReadOnlyObservableCollection<AtisStationViewModel> CompactWindowStations { get; set; }

    public ReactiveCommand<Unit, Unit> GetDigitalAtisLetterCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSettingsDialogCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenProfileConfigurationWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> EndClientSessionCommand { get; }
    public ReactiveCommand<Unit, Unit> InvokeCompactViewCommand { get; }

    #region Reactive Properties
    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    private string _currentTime = DateTime.UtcNow.ToString("HH:mm/ss", CultureInfo.InvariantCulture);
    public string CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    private bool _showOverlay;
    public bool ShowOverlay
    {
        get => _showOverlay;
        set => this.RaiseAndSetIfChanged(ref _showOverlay, value);
    }
    #endregion

    public MainWindowViewModel(ISessionManager sessionManager, IWindowFactory windowFactory,
        IViewModelFactory viewModelFactory, IWindowLocationService windowLocationService,
        IAtisHubConnection atisHubConnection, IWebsocketService websocketService)
    {
        _sessionManager = sessionManager;
        _windowFactory = windowFactory;
        _viewModelFactory = viewModelFactory;
        _windowLocationService = windowLocationService;
        _websocketService = websocketService;
        _atisHubConnection = atisHubConnection;

        OpenSettingsDialogCommand = ReactiveCommand.Create(OpenSettingsDialog);
        OpenProfileConfigurationWindowCommand = ReactiveCommand.CreateFromTask(OpenProfileConfigurationWindow);
        EndClientSessionCommand = ReactiveCommand.CreateFromTask(EndClientSession);
        InvokeCompactViewCommand = ReactiveCommand.Create(InvokeCompactView);
        GetDigitalAtisLetterCommand = ReactiveCommand.CreateFromTask(HandleGetDigitalAtisLetter);

        _disposables.Add(OpenSettingsDialogCommand);
        _disposables.Add(OpenProfileConfigurationWindowCommand);
        _disposables.Add(EndClientSessionCommand);
        _disposables.Add(InvokeCompactViewCommand);
        _disposables.Add(GetDigitalAtisLetterCommand);

        _atisStationSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
            .Do(_ =>
            {
                if (AtisStations != null && AtisStations.Count > SelectedTabIndex)
                {
                    _beforeStationSortSelectedStationId = AtisStations[SelectedTabIndex].Id;
                }
            })
            .Sort(SortExpressionComparer<AtisStationViewModel>
                .Ascending(i => i.NetworkConnectionStatus switch
                {
                    NetworkConnectionStatus.Connected => 0,
                    NetworkConnectionStatus.Observer => 0,
                    _ => 1
                })
                .ThenBy(i => i.Identifier ?? string.Empty))
            .Bind(out var sortedStations)
            .Subscribe(_ =>
            {
                AtisStations = sortedStations;
                if (_beforeStationSortSelectedStationId == null || AtisStations.Count == 0)
                {
                    SelectedTabIndex = 0; // No valid previous station or empty list
                }
                else
                {
                    var selectedStation = AtisStations.FirstOrDefault(
                        it => it.Id == _beforeStationSortSelectedStationId);

                    SelectedTabIndex = selectedStation != null
                        ? AtisStations.IndexOf(selectedStation)
                        : 0; // Default to the first tab if no match
                }
            });

        AtisStations = sortedStations;

        _atisStationSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
            .Filter(x => x.NetworkConnectionStatus is NetworkConnectionStatus.Connected or NetworkConnectionStatus.Observer)
            .Sort(SortExpressionComparer<AtisStationViewModel>.Ascending(i => i.Identifier ?? string.Empty))
            .Bind(out var connectedStations)
            .Subscribe(_ =>
            {
                CompactWindowStations = connectedStations;
            });

        CompactWindowStations = connectedStations;

        MessageBus.Current.Listen<OpenGenerateSettingsDialog>().Subscribe(_ => OpenSettingsDialog());
        MessageBus.Current.Listen<AtisStationAdded>().Subscribe(evt =>
        {
            if (_sessionManager.CurrentProfile?.Stations == null)
                return;

            var station = _sessionManager.CurrentProfile?.Stations.FirstOrDefault(x => x.Id == evt.Id);
            if (station != null && _atisStationSource.Items.All(x => x.Id != station.Id))
            {
                var atisStationViewModel = _viewModelFactory.CreateAtisStationViewModel(station);
                _disposables.Add(atisStationViewModel);
                _atisStationSource.Add(atisStationViewModel);
            }
        });
        MessageBus.Current.Listen<AtisStationUpdated>().Subscribe(evt =>
        {
            var station = _atisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
            if (station != null)
            {
                station.Disconnect();

                _disposables.Remove(station);
                _atisStationSource.Remove(station);

                var updatedStation = _sessionManager.CurrentProfile?.Stations?.FirstOrDefault(x => x.Id == evt.Id);
                if (updatedStation != null)
                {
                    var atisStationViewModel = _viewModelFactory.CreateAtisStationViewModel(updatedStation);
                    _disposables.Add(atisStationViewModel);
                    _atisStationSource.Add(atisStationViewModel);
                }
            }
        });
        MessageBus.Current.Listen<AtisStationDeleted>().Subscribe(evt =>
        {
            var station = _atisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
            if (station != null)
            {
                _atisStationSource.Remove(station);
            }
        });

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                ShowOverlay = lifetime.Windows.Count > 1;
            };
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => CurrentTime = DateTime.UtcNow.ToString("HH:mm/ss", CultureInfo.InvariantCulture);
        timer.Start();
    }

    private async Task HandleGetDigitalAtisLetter()
    {
        // This should never happen... but then again, I've been wrong before
        if (SelectedTabIndex < 0 || SelectedTabIndex >= AtisStations.Count)
            return;

        var selectedStation = AtisStations[SelectedTabIndex];

        if (string.IsNullOrEmpty(selectedStation.Identifier))
            return;

        var requestDto = new DigitalAtisRequestDto
        {
            Id = selectedStation.Identifier,
            AtisType = selectedStation.AtisType
        };
        var atisLetter = await _atisHubConnection.GetDigitalAtisLetter(requestDto);
        if (atisLetter != null)
        {
            selectedStation.SetAtisLetterCommand.Execute(atisLetter.Value).Subscribe();
        }
    }

    public async Task PopulateAtisStations()
    {
        if (_sessionManager.CurrentProfile?.Stations == null)
            return;

        foreach (var station in _sessionManager.CurrentProfile.Stations.OrderBy(x => x.Identifier))
        {
            try
            {
                if (_atisStationSource.Items.FirstOrDefault(x => x.Id == station.Id) == null)
                {
                    var atisStationViewModel = _viewModelFactory.CreateAtisStationViewModel(station);
                    _disposables.Add(atisStationViewModel);
                    _atisStationSource.Add(atisStationViewModel);
                }
            }
            catch (Exception ex)
            {
                if (Owner != null)
                {
                    await MessageBox.ShowDialog(Owner, "Error populating ATIS station: " + ex.Message, "Error",
                        MessageBoxButton.Ok, MessageBoxIcon.Error);
                }
            }
        }
    }

    private async Task OpenProfileConfigurationWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        if (lifetime.MainWindow == null)
            return;

        var window = _windowFactory.CreateProfileConfigurationWindow();
        window.Topmost = lifetime.MainWindow.Topmost;
        await window.ShowDialog(lifetime.MainWindow);
    }

    private void InvokeCompactView()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        lifetime.MainWindow?.Hide();

        var compactView = _windowFactory.CreateCompactWindow();
        if (compactView.DataContext is CompactWindowViewModel context)
        {
            context.Stations = CompactWindowStations;
        }

        compactView.Show();
    }

    private void OpenSettingsDialog()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            var dialog = _windowFactory.CreateSettingsDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            dialog.ShowDialog(lifetime.MainWindow);
        }
    }

    private async Task EndClientSession()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
                return;

            if (AtisStations.Any(x => x.NetworkConnectionStatus == NetworkConnectionStatus.Connected))
            {
                if (await MessageBox.ShowDialog(lifetime.MainWindow,
                        "You still have active ATIS connections. Are you sure you want to exit?", "Confirm",
                        MessageBoxButton.YesNo, MessageBoxIcon.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            _sessionManager.EndSession();
        }
    }

    public void UpdatePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Restore(window);
    }

    public async Task StartWebsocket()
    {
        await _websocketService.StartAsync();
    }

    public async Task StopWebsocket()
    {
        await _websocketService.StopAsync();
    }

    public async Task ConnectToHub()
    {
        await _atisHubConnection.Connect();
    }

    public async Task DisconnectFromHub()
    {
        await _atisHubConnection.Disconnect();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _disposables.Dispose();
    }
}
