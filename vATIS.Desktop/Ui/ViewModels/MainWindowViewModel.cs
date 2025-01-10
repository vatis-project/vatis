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
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;

namespace Vatsim.Vatis.Ui.ViewModels;

public class MainWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAtisHubConnection _atisHubConnection;
    private readonly SourceList<AtisStationViewModel> _atisStationSource = new();
    private readonly CompositeDisposable _disposables = new();
    private readonly ISessionManager _sessionManager;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IWebsocketService _websocketService;
    private readonly IWindowFactory _windowFactory;
    private readonly IWindowLocationService _windowLocationService;
    private string? _beforeStationSortSelectedStationId;

    public MainWindowViewModel(
        ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IViewModelFactory viewModelFactory,
        IWindowLocationService windowLocationService,
        IAtisHubConnection atisHubConnection,
        IWebsocketService websocketService)
    {
        this._sessionManager = sessionManager;
        this._windowFactory = windowFactory;
        this._viewModelFactory = viewModelFactory;
        this._windowLocationService = windowLocationService;
        this._websocketService = websocketService;
        this._atisHubConnection = atisHubConnection;

        this.OpenSettingsDialogCommand = ReactiveCommand.Create(this.OpenSettingsDialog);
        this.OpenProfileConfigurationWindowCommand =
            ReactiveCommand.CreateFromTask(this.OpenProfileConfigurationWindow);
        this.EndClientSessionCommand = ReactiveCommand.CreateFromTask(this.EndClientSession);
        this.InvokeCompactViewCommand = ReactiveCommand.Create(this.InvokeCompactView);

        this._disposables.Add(this.OpenSettingsDialogCommand);
        this._disposables.Add(this.OpenProfileConfigurationWindowCommand);
        this._disposables.Add(this.EndClientSessionCommand);
        this._disposables.Add(this.InvokeCompactViewCommand);

        this._atisStationSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
            .Do(
                _ =>
                {
                    if (this.AtisStations != null && this.AtisStations.Count > this.SelectedTabIndex)
                    {
                        this._beforeStationSortSelectedStationId = this.AtisStations[this.SelectedTabIndex].Id;
                    }
                })
            .Sort(
                SortExpressionComparer<AtisStationViewModel>
                    .Ascending(
                        i => i.NetworkConnectionStatus switch
                        {
                            NetworkConnectionStatus.Connected => 0,
                            NetworkConnectionStatus.Observer => 0,
                            _ => 1
                        })
                    .ThenBy(i => i.Identifier ?? string.Empty))
            .Bind(out var sortedStations)
            .Subscribe(
                _ =>
                {
                    this.AtisStations = sortedStations;
                    if (this._beforeStationSortSelectedStationId == null || this.AtisStations.Count == 0)
                    {
                        this.SelectedTabIndex = 0; // No valid previous station or empty list
                    }
                    else
                    {
                        var selectedStation = this.AtisStations.FirstOrDefault(
                            it => it.Id == this._beforeStationSortSelectedStationId);

                        this.SelectedTabIndex = selectedStation != null
                            ? this.AtisStations.IndexOf(selectedStation)
                            : 0; // Default to the first tab if no match
                    }
                });

        this.AtisStations = sortedStations;

        this._atisStationSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
            .Filter(
                x => x.NetworkConnectionStatus is NetworkConnectionStatus.Connected or NetworkConnectionStatus.Observer)
            .Sort(SortExpressionComparer<AtisStationViewModel>.Ascending(i => i.Identifier ?? string.Empty))
            .Bind(out var connectedStations)
            .Subscribe(_ => { this.CompactWindowStations = connectedStations; });

        this.CompactWindowStations = connectedStations;

        MessageBus.Current.Listen<OpenGenerateSettingsDialog>().Subscribe(_ => this.OpenSettingsDialog());
        MessageBus.Current.Listen<AtisStationAdded>().Subscribe(
            evt =>
            {
                if (this._sessionManager.CurrentProfile?.Stations == null)
                {
                    return;
                }

                var station = this._sessionManager.CurrentProfile?.Stations.FirstOrDefault(x => x.Id == evt.Id);
                if (station != null && this._atisStationSource.Items.All(x => x.Id != station.Id))
                {
                    var atisStationViewModel = this._viewModelFactory.CreateAtisStationViewModel(station);
                    this._disposables.Add(atisStationViewModel);
                    this._atisStationSource.Add(atisStationViewModel);
                }
            });
        MessageBus.Current.Listen<AtisStationUpdated>().Subscribe(
            evt =>
            {
                var station = this._atisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
                if (station != null)
                {
                    station.Disconnect();

                    this._disposables.Remove(station);
                    this._atisStationSource.Remove(station);

                    var updatedStation =
                        this._sessionManager.CurrentProfile?.Stations?.FirstOrDefault(x => x.Id == evt.Id);
                    if (updatedStation != null)
                    {
                        var atisStationViewModel = this._viewModelFactory.CreateAtisStationViewModel(updatedStation);
                        this._disposables.Add(atisStationViewModel);
                        this._atisStationSource.Add(atisStationViewModel);
                    }
                }
            });
        MessageBus.Current.Listen<AtisStationDeleted>().Subscribe(
            evt =>
            {
                var station = this._atisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
                if (station != null)
                {
                    this._atisStationSource.Remove(station);
                }
            });

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            ((INotifyCollectionChanged)lifetime.Windows).CollectionChanged += (_, _) =>
            {
                this.ShowOverlay = lifetime.Windows.Count > 1;
            };
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => this.CurrentTime = DateTime.UtcNow.ToString("HH:mm/ss", CultureInfo.InvariantCulture);
        timer.Start();
    }

    public Window? Owner { get; set; }

    public ReadOnlyObservableCollection<AtisStationViewModel> AtisStations { get; set; }

    private ReadOnlyObservableCollection<AtisStationViewModel> CompactWindowStations { get; set; }

    public ReactiveCommand<Unit, Unit> OpenSettingsDialogCommand { get; }

    public ReactiveCommand<Unit, Unit> OpenProfileConfigurationWindowCommand { get; }

    public ReactiveCommand<Unit, Unit> EndClientSessionCommand { get; }

    public ReactiveCommand<Unit, Unit> InvokeCompactViewCommand { get; }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this._disposables.Dispose();
    }

    public async Task PopulateAtisStations()
    {
        if (this._sessionManager.CurrentProfile?.Stations == null)
        {
            return;
        }

        foreach (var station in this._sessionManager.CurrentProfile.Stations.OrderBy(x => x.Identifier))
        {
            try
            {
                if (this._atisStationSource.Items.FirstOrDefault(x => x.Id == station.Id) == null)
                {
                    var atisStationViewModel = this._viewModelFactory.CreateAtisStationViewModel(station);
                    this._disposables.Add(atisStationViewModel);
                    this._atisStationSource.Add(atisStationViewModel);
                }
            }
            catch (Exception ex)
            {
                if (this.Owner != null)
                {
                    await MessageBox.ShowDialog(
                        this.Owner,
                        "Error populating ATIS station: " + ex.Message,
                        "Error",
                        MessageBoxButton.Ok,
                        MessageBoxIcon.Error);
                }
            }
        }
    }

    private async Task OpenProfileConfigurationWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return;
        }

        if (lifetime.MainWindow == null)
        {
            return;
        }

        var window = this._windowFactory.CreateProfileConfigurationWindow();
        window.Topmost = lifetime.MainWindow.Topmost;
        await window.ShowDialog(lifetime.MainWindow);
    }

    private void InvokeCompactView()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return;
        }

        lifetime.MainWindow?.Hide();

        var compactView = this._windowFactory.CreateCompactWindow();
        if (compactView.DataContext is CompactWindowViewModel context)
        {
            context.Stations = this.CompactWindowStations;
        }

        compactView.Show();
    }

    private void OpenSettingsDialog()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            var dialog = this._windowFactory.CreateSettingsDialog();
            dialog.Topmost = lifetime.MainWindow.Topmost;
            dialog.ShowDialog(lifetime.MainWindow);
        }
    }

    private async Task EndClientSession()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow == null)
            {
                return;
            }

            if (this.AtisStations.Any(x => x.NetworkConnectionStatus == NetworkConnectionStatus.Connected))
            {
                if (await MessageBox.ShowDialog(
                        lifetime.MainWindow,
                        "You still have active ATIS connections. Are you sure you want to exit?",
                        "Confirm",
                        MessageBoxButton.YesNo,
                        MessageBoxIcon.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            this._sessionManager.EndSession();
        }
    }

    public void UpdatePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this._windowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this._windowLocationService.Restore(window);
    }

    public async Task StartWebsocket()
    {
        await this._websocketService.StartAsync();
    }

    public async Task StopWebsocket()
    {
        await this._websocketService.StopAsync();
    }

    public async Task ConnectToHub()
    {
        await this._atisHubConnection.Connect();
    }

    public async Task DisconnectFromHub()
    {
        await this._atisHubConnection.Disconnect();
    }

    #region Reactive Properties

    private int _selectedTabIndex;

    public int SelectedTabIndex
    {
        get => this._selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref this._selectedTabIndex, value);
    }

    private string _currentTime = DateTime.UtcNow.ToString("HH:mm/ss", CultureInfo.InvariantCulture);

    public string CurrentTime
    {
        get => this._currentTime;
        set => this.RaiseAndSetIfChanged(ref this._currentTime, value);
    }

    private bool _showOverlay;

    public bool ShowOverlay
    {
        get => this._showOverlay;
        set => this.RaiseAndSetIfChanged(ref this._showOverlay, value);
    }

    #endregion
}