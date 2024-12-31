using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reactive;
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

public class MainWindowViewModel : ReactiveViewModelBase
{
    private readonly ISessionManager mSessionManager;
    private readonly IWindowFactory mWindowFactory;
    private readonly IViewModelFactory mViewModelFactory;
    private readonly IWindowLocationService mWindowLocationService;
    private readonly IAtisHubConnection mAtisHubConnection;
    private readonly IWebsocketService mWebsocketService;

    public Window? Owner { get; set; }
    private readonly SourceList<AtisStationViewModel> mAtisStationSource = new();
    public ReadOnlyObservableCollection<AtisStationViewModel> AtisStations { get; set; }
    private ReadOnlyObservableCollection<AtisStationViewModel> CompactWindowStations { get; set; }

    public ReactiveCommand<Unit, Unit> OpenSettingsDialogCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> OpenProfileConfigurationWindowCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> EndClientSessionCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> InvokeCompactViewCommand { get; private set; }

    #region Reactive Properties

    private string mCurrentTime = DateTime.UtcNow.ToString("HH:mm/ss", CultureInfo.InvariantCulture);

    private int mSelectedTabIndex;
    public int SelectedTabIndex
    {
        get => mSelectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref mSelectedTabIndex, value);
    }

    public string CurrentTime
    {
        get => mCurrentTime;
        set => this.RaiseAndSetIfChanged(ref mCurrentTime, value);
    }

    private bool mShowOverlay;

    public bool ShowOverlay
    {
        get => mShowOverlay;
        set => this.RaiseAndSetIfChanged(ref mShowOverlay, value);
    }

    #endregion

    public MainWindowViewModel(ISessionManager sessionManager, IWindowFactory windowFactory,
        IViewModelFactory viewModelFactory, IWindowLocationService windowLocationService,
        IAtisHubConnection atisHubConnection, IWebsocketService websocketService)
    {
        mSessionManager = sessionManager;
        mWindowFactory = windowFactory;
        mViewModelFactory = viewModelFactory;
        mWindowLocationService = windowLocationService;
        mAtisHubConnection = atisHubConnection;
        mWebsocketService = websocketService;

        OpenSettingsDialogCommand = ReactiveCommand.Create(OpenSettingsDialog);
        OpenProfileConfigurationWindowCommand = ReactiveCommand.CreateFromTask(OpenProfileConfigurationWindow);
        EndClientSessionCommand = ReactiveCommand.CreateFromTask(EndClientSession);
        InvokeCompactViewCommand = ReactiveCommand.Create(InvokeCompactView);

        mAtisStationSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
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
                SelectedTabIndex = 0;
            });

        AtisStations = sortedStations;

        mAtisStationSource.Connect()
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
            if (mSessionManager.CurrentProfile?.Stations == null)
                return;

            var station = mSessionManager.CurrentProfile?.Stations.FirstOrDefault(x => x.Id == evt.Id);
            if (station != null && mAtisStationSource.Items.All(x => x.Id != station.Id))
            {
                mAtisStationSource.Add(mViewModelFactory.CreateAtisStationViewModel(station));
            }
        });
        MessageBus.Current.Listen<AtisStationUpdated>().Subscribe(evt =>
        {
            var station = mAtisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
            if (station != null)
            {
                station.Disconnect();

                mAtisStationSource.Remove(station);

                var updatedStation = mSessionManager.CurrentProfile?.Stations?.FirstOrDefault(x => x.Id == evt.Id);
                if (updatedStation != null)
                {
                    mAtisStationSource.Add(mViewModelFactory.CreateAtisStationViewModel(updatedStation));
                }
            }
        });
        MessageBus.Current.Listen<AtisStationDeleted>().Subscribe(evt =>
        {
            var station = mAtisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
            if (station != null)
            {
                mAtisStationSource.Remove(station);
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

    public async Task PopulateAtisStations()
    {
        if (mSessionManager.CurrentProfile?.Stations == null)
            return;

        foreach (var station in mSessionManager.CurrentProfile.Stations.OrderBy(x => x.Identifier))
        {
            try
            {
                if (mAtisStationSource.Items.FirstOrDefault(x => x.Id == station.Id) == null)
                {
                    mAtisStationSource.Add(mViewModelFactory.CreateAtisStationViewModel(station));
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

        var window = mWindowFactory.CreateProfileConfigurationWindow();
        window.Topmost = lifetime.MainWindow.Topmost;
        await window.ShowDialog(lifetime.MainWindow);
    }

    private void InvokeCompactView()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
            return;

        lifetime.MainWindow?.Hide();

        var compactView = mWindowFactory.CreateCompactWindow();
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

            var dialog = mWindowFactory.CreateSettingsDialog();
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

            mSessionManager.EndSession();
        }
    }

    public void UpdatePosition(Window? window)
    {
        if (window == null)
            return;

        mWindowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        mWindowLocationService.Restore(window);
    }

    public async Task StartWebsocket()
    {
        await mWebsocketService.StartAsync();
    }

    public async Task StopWebsocket()
    {
        await mWebsocketService.StopAsync();
    }

    public async Task ConnectToHub()
    {
        await mAtisHubConnection.Connect();
    }

    public async Task DisconnectFromHub()
    {
        await mAtisHubConnection.Disconnect();
    }
}