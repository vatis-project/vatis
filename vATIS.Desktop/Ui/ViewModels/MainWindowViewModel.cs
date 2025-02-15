// <copyright file="MainWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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
using Serilog;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Events.EventBus;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the main application window, providing properties,
/// commands, and methods for managing the UI and application behavior.
/// </summary>
public class MainWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ISessionManager _sessionManager;
    private readonly IWindowFactory _windowFactory;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly IWindowLocationService _windowLocationService;
    private readonly IAtisHubConnection _atisHubConnection;
    private readonly IWebsocketService _websocketService;
    private readonly SourceList<AtisStationViewModel> _atisStationSource = new();
    private List<string> _previousKeys = new();
    private string? _beforeStationSortSelectedStationId;
    private int _selectedTabIndex;
    private string _currentTime = DateTime.UtcNow.ToString("HH:mm/ss", CultureInfo.InvariantCulture);
    private bool _showOverlay;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager for managing user sessions.</param>
    /// <param name="windowFactory">The factory responsible for creating windows.</param>
    /// <param name="viewModelFactory">The factory responsible for creating view models.</param>
    /// <param name="windowLocationService">The service responsible for managing window locations.</param>
    /// <param name="atisHubConnection">The connection to the ATIS hub for fetching station data.</param>
    /// <param name="websocketService">The service responsible for managing WebSocket communication.</param>
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

        _disposables.Add(OpenSettingsDialogCommand);
        _disposables.Add(OpenProfileConfigurationWindowCommand);
        _disposables.Add(EndClientSessionCommand);
        _disposables.Add(InvokeCompactViewCommand);

        _atisStationSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
            .AutoRefresh(x => x.Ordinal)
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
                    NetworkConnectionStatus.Observer => 1,
                    _ => 2
                })
                .ThenBy(i => i.Ordinal)
                .ThenBy(i => i.Identifier ?? string.Empty)
                .ThenBy(i => i.AtisType switch
                {
                    AtisType.Combined => 0,
                    AtisType.Arrival => 1,
                    AtisType.Departure => 2,
                    _ => 3
                }))
            .Bind(out var sortedStations)
            .Subscribe(_ =>
            {
                // Generate composite keys using Identifier + Ordinal
                var currentKeys = sortedStations.Select(s => $"{s.Identifier}_{s.AtisType}_{s.Ordinal}").ToList();
                var keysChanged = !_previousKeys.SequenceEqual(currentKeys);
                _previousKeys = currentKeys;

                AtisStations = sortedStations;

                if (keysChanged || _beforeStationSortSelectedStationId == null || AtisStations.Count == 0)
                {
                    SelectedTabIndex = 0; // Reset if ordinals change or no valid previous station
                }
                else
                {
                    var selectedStation = AtisStations.FirstOrDefault(
                        it => it.Id == _beforeStationSortSelectedStationId);

                    SelectedTabIndex = selectedStation != null
                        ? AtisStations.IndexOf(selectedStation)
                        : 0; // Default to first tab if no match
                }
            });

        AtisStations = sortedStations;

        _atisStationSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
            .AutoRefresh(x => x.Ordinal)
            .Filter(x => x.NetworkConnectionStatus is NetworkConnectionStatus.Connected or NetworkConnectionStatus.Observer)
            .Sort(SortExpressionComparer<AtisStationViewModel>
                .Ascending(i => i.Ordinal)
                .ThenBy(i => i.Identifier ?? string.Empty)
                .ThenBy(i => i.AtisType switch
                {
                    AtisType.Combined => 0,
                    AtisType.Arrival => 1,
                    AtisType.Departure => 2,
                    _ => 3
                }))
            .Bind(out var connectedStations)
            .Subscribe(_ => { CompactWindowStations = connectedStations; });

        CompactWindowStations = connectedStations;

        _disposables.Add(EventBus.Instance.Subscribe<OpenGenerateSettingsDialog>(_ => OpenSettingsDialog()));
        _disposables.Add(EventBus.Instance.Subscribe<AtisStationAdded>(evt =>
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
        }));
        _disposables.Add(EventBus.Instance.Subscribe<AtisStationUpdated>(evt =>
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
        }));
        _disposables.Add(EventBus.Instance.Subscribe<AtisStationDeleted>(evt =>
        {
            var station = _atisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
            if (station != null)
            {
                _atisStationSource.Remove(station);
            }
        }));
        _disposables.Add(EventBus.Instance.Subscribe<AtisStationOrdinalChanged>(evt =>
        {
            var station = _atisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
            if (station != null)
            {
                station.Ordinal = evt.NewOrdinal;
            }
        }));
        _disposables.Add(EventBus.Instance.Subscribe<GeneralSettingsUpdated>(_ =>
        {
            Debug.WriteLine("GeneralSettingsUpdated");
        }));

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

    /// <summary>
    /// Gets or sets the owner window of the view model.
    /// </summary>
    public Window? Owner { get; set; }

    /// <summary>
    /// Gets or sets the collection of ATIS station view models to display in the tab control.
    /// </summary>
    public ReadOnlyObservableCollection<AtisStationViewModel> AtisStations { get; set; }

    /// <summary>
    /// Gets or sets the filtered collection of ATIS stations displayed in the compact window.
    /// </summary>
    public ReadOnlyObservableCollection<AtisStationViewModel> CompactWindowStations { get; set; }

    /// <summary>
    /// Gets the command to open the settings dialog.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenSettingsDialogCommand { get; }

    /// <summary>
    /// Gets the command to open the profile configuration window.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenProfileConfigurationWindowCommand { get; }

    /// <summary>
    /// Gets the command to end the current client session.
    /// </summary>
    public ReactiveCommand<Unit, Unit> EndClientSessionCommand { get; }

    /// <summary>
    /// Gets the command to invoke the compact view mode.
    /// </summary>
    public ReactiveCommand<Unit, Unit> InvokeCompactViewCommand { get; }

    /// <summary>
    /// Gets or sets the index of the currently selected tab in the tab control.
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedTabIndex, value);
    }

    /// <summary>
    /// Gets or sets the current time in a formatted string.
    /// </summary>
    public string CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the overlay is displayed on the main application window.
    /// </summary>
    public bool ShowOverlay
    {
        get => _showOverlay;
        set => this.RaiseAndSetIfChanged(ref _showOverlay, value);
    }

    /// <summary>
    /// Updates the stored position of the specified window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position is to be updated. If null, no update is performed.</param>
    public void UpdatePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Update(window);
    }

    /// <summary>
    /// Restores the position of the specified window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position is to be restored. If null, the method performs no action.</param>
    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Restore(window);
    }

    /// <summary>
    /// Starts the WebSocket connection using the underlying WebSocket service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartWebsocket()
    {
        await _websocketService.StartAsync();
    }

    /// <summary>
    /// Stops the WebSocket connection asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StopWebsocket()
    {
        await _websocketService.StopAsync();
    }

    /// <summary>
    /// Connects to the ATIS hub using the provided hub connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of connecting to the ATIS hub.</returns>
    public async Task ConnectToHub()
    {
        await _atisHubConnection.Connect();
    }

    /// <summary>
    /// Disconnects from the ATIS hub connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of disconnecting from the hub.</returns>
    public async Task DisconnectFromHub()
    {
        await _atisHubConnection.Disconnect();
    }

    /// <summary>
    /// Populates the ATIS stations collection by creating and adding <see cref="AtisStationViewModel"/> instances
    /// based on stations defined in the current profile.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
                Log.Error(ex, "Error Populating ATIS Station {StationId} {Identifier}", station.Id, station.Identifier);
                if (Owner != null)
                {
                    await MessageBox.ShowDialog(Owner, "Error populating ATIS station: " + ex.Message, "Error",
                        MessageBoxButton.Ok, MessageBoxIcon.Error);
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _disposables.Dispose();
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
}
