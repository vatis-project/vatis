// <copyright file="MainWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <summary>
/// Represents the view model for the main application window, providing properties, commands, and methods for managing the UI and application behavior.
/// </summary>
public class MainWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IAtisHubConnection atisHubConnection;
    private readonly SourceList<AtisStationViewModel> atisStationSource = new();
    private readonly CompositeDisposable disposables = new();
    private readonly ISessionManager sessionManager;
    private readonly IViewModelFactory viewModelFactory;
    private readonly IWebsocketService websocketService;
    private readonly IWindowFactory windowFactory;
    private readonly IWindowLocationService windowLocationService;
    private string? beforeStationSortSelectedStationId;
    private int selectedTabIndex;
    private string currentTime = DateTime.UtcNow.ToString("HH:mm/ss", CultureInfo.InvariantCulture);
    private bool showOverlay;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager for managing user sessions.</param>
    /// <param name="windowFactory">The factory responsible for creating windows.</param>
    /// <param name="viewModelFactory">The factory responsible for creating view models.</param>
    /// <param name="windowLocationService">The service responsible for managing window locations.</param>
    /// <param name="atisHubConnection">The connection to the ATIS hub for fetching station data.</param>
    /// <param name="websocketService">The service responsible for managing WebSocket communication.</param>
    public MainWindowViewModel(
        ISessionManager sessionManager,
        IWindowFactory windowFactory,
        IViewModelFactory viewModelFactory,
        IWindowLocationService windowLocationService,
        IAtisHubConnection atisHubConnection,
        IWebsocketService websocketService)
    {
        this.sessionManager = sessionManager;
        this.windowFactory = windowFactory;
        this.viewModelFactory = viewModelFactory;
        this.windowLocationService = windowLocationService;
        this.websocketService = websocketService;
        this.atisHubConnection = atisHubConnection;

        this.OpenSettingsDialogCommand = ReactiveCommand.Create(this.OpenSettingsDialog);
        this.OpenProfileConfigurationWindowCommand =
            ReactiveCommand.CreateFromTask(this.OpenProfileConfigurationWindow);
        this.EndClientSessionCommand = ReactiveCommand.CreateFromTask(this.EndClientSession);
        this.InvokeCompactViewCommand = ReactiveCommand.Create(this.InvokeCompactView);

        this.disposables.Add(this.OpenSettingsDialogCommand);
        this.disposables.Add(this.OpenProfileConfigurationWindowCommand);
        this.disposables.Add(this.EndClientSessionCommand);
        this.disposables.Add(this.InvokeCompactViewCommand);

        this.atisStationSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
            .Do(
                _ =>
                {
                    if (this.AtisStations != null && this.AtisStations.Count > this.SelectedTabIndex)
                    {
                        this.beforeStationSortSelectedStationId = this.AtisStations[this.SelectedTabIndex].Id;
                    }
                })
            .Sort(
                SortExpressionComparer<AtisStationViewModel>
                    .Ascending(
                        i => i.NetworkConnectionStatus switch
                        {
                            NetworkConnectionStatus.Connected => 0,
                            NetworkConnectionStatus.Observer => 0,
                            _ => 1,
                        })
                    .ThenBy(i => i.Identifier ?? string.Empty))
            .Bind(out var sortedStations)
            .Subscribe(
                _ =>
                {
                    this.AtisStations = sortedStations;
                    if (this.beforeStationSortSelectedStationId == null || this.AtisStations.Count == 0)
                    {
                        this.SelectedTabIndex = 0; // No valid previous station or empty list
                    }
                    else
                    {
                        var selectedStation = this.AtisStations.FirstOrDefault(
                            it => it.Id == this.beforeStationSortSelectedStationId);

                        this.SelectedTabIndex = selectedStation != null
                            ? this.AtisStations.IndexOf(selectedStation)
                            : 0; // Default to the first tab if no match
                    }
                });

        this.AtisStations = sortedStations;

        this.atisStationSource.Connect()
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
                if (this.sessionManager.CurrentProfile?.Stations == null)
                {
                    return;
                }

                var station = this.sessionManager.CurrentProfile?.Stations.FirstOrDefault(x => x.Id == evt.Id);
                if (station != null && this.atisStationSource.Items.All(x => x.Id != station.Id))
                {
                    var atisStationViewModel = this.viewModelFactory.CreateAtisStationViewModel(station);
                    this.disposables.Add(atisStationViewModel);
                    this.atisStationSource.Add(atisStationViewModel);
                }
            });
        MessageBus.Current.Listen<AtisStationUpdated>().Subscribe(
            evt =>
            {
                var station = this.atisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
                if (station != null)
                {
                    station.Disconnect();

                    this.disposables.Remove(station);
                    this.atisStationSource.Remove(station);

                    var updatedStation =
                        this.sessionManager.CurrentProfile?.Stations?.FirstOrDefault(x => x.Id == evt.Id);
                    if (updatedStation != null)
                    {
                        var atisStationViewModel = this.viewModelFactory.CreateAtisStationViewModel(updatedStation);
                        this.disposables.Add(atisStationViewModel);
                        this.atisStationSource.Add(atisStationViewModel);
                    }
                }
            });
        MessageBus.Current.Listen<AtisStationDeleted>().Subscribe(
            evt =>
            {
                var station = this.atisStationSource.Items.FirstOrDefault(x => x.Id == evt.Id);
                if (station != null)
                {
                    this.atisStationSource.Remove(station);
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
        get => this.selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref this.selectedTabIndex, value);
    }

    /// <summary>
    /// Gets or sets the current time in a formatted string.
    /// </summary>
    public string CurrentTime
    {
        get => this.currentTime;
        set => this.RaiseAndSetIfChanged(ref this.currentTime, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the overlay is displayed on the main application window.
    /// </summary>
    public bool ShowOverlay
    {
        get => this.showOverlay;
        set => this.RaiseAndSetIfChanged(ref this.showOverlay, value);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.disposables.Dispose();
    }

    /// <summary>
    /// Populates the ATIS stations collection by creating and adding <see cref="AtisStationViewModel"/> instances
    /// based on stations defined in the current profile.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task PopulateAtisStations()
    {
        if (this.sessionManager.CurrentProfile?.Stations == null)
        {
            return;
        }

        foreach (var station in this.sessionManager.CurrentProfile.Stations.OrderBy(x => x.Identifier))
        {
            try
            {
                if (this.atisStationSource.Items.FirstOrDefault(x => x.Id == station.Id) == null)
                {
                    var atisStationViewModel = this.viewModelFactory.CreateAtisStationViewModel(station);
                    this.disposables.Add(atisStationViewModel);
                    this.atisStationSource.Add(atisStationViewModel);
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

    /// <summary>
    /// Updates the stored position of the specified window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position is to be updated. If null, no update is performed.</param>
    public void UpdatePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this.windowLocationService.Update(window);
    }

    /// <summary>
    /// Restores the position of the specified window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position is to be restored. If null, the method performs no action.</param>
    public void RestorePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this.windowLocationService.Restore(window);
    }

    /// <summary>
    /// Starts the WebSocket connection using the underlying WebSocket service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StartWebsocket()
    {
        await this.websocketService.StartAsync();
    }

    /// <summary>
    /// Stops the WebSocket connection asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task StopWebsocket()
    {
        await this.websocketService.StopAsync();
    }

    /// <summary>
    /// Connects to the ATIS hub using the provided hub connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of connecting to the ATIS hub.</returns>
    public async Task ConnectToHub()
    {
        await this.atisHubConnection.Connect();
    }

    /// <summary>
    /// Disconnects from the ATIS hub connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation of disconnecting from the hub.</returns>
    public async Task DisconnectFromHub()
    {
        await this.atisHubConnection.Disconnect();
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

        var window = this.windowFactory.CreateProfileConfigurationWindow();
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

        var compactView = this.windowFactory.CreateCompactWindow();
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

            var dialog = this.windowFactory.CreateSettingsDialog();
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

            this.sessionManager.EndSession();
        }
    }
}
