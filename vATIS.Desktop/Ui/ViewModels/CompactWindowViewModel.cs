// <copyright file="CompactWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the compact window in the UI.
/// </summary>
public class CompactWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ISessionManager _sessionManager;
    private readonly IWindowLocationService _windowLocationService;
    private readonly ObservableCollection<AtisStationViewModel> _filteredStationsSource = [];
    private ReadOnlyObservableCollection<AtisStationViewModel> _stations = new([]);
    private ReadOnlyObservableCollection<AtisStationViewModel> _filteredStations;
    private IDialogOwner? _dialogOwner;
    private bool _isControlsVisible;
    private bool _hasAnyStations;
    private bool _statusLabelVisible;
    private string? _statusLabel;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactWindowViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager service.</param>
    /// <param name="windowLocationService">The service responsible for managing window locations.</param>
    public CompactWindowViewModel(ISessionManager sessionManager, IWindowLocationService windowLocationService)
    {
        _sessionManager = sessionManager;
        _windowLocationService = windowLocationService;

        _filteredStations = new ReadOnlyObservableCollection<AtisStationViewModel>(_filteredStationsSource);

        InvokeMainWindowCommand = ReactiveCommand.Create<ICloseable>(InvokeMainWindow);
        EndClientSessionCommand = ReactiveCommand.CreateFromTask(HandleEndClientSession);
    }

    /// <summary>
    /// Gets the command used to invoke the main window logic.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> InvokeMainWindowCommand { get; }

    /// <summary>
    /// Gets the command to end the current client session.
    /// </summary>
    public ReactiveCommand<Unit, bool> EndClientSessionCommand { get; }

    /// <summary>
    /// Gets or sets a value indicating whether there are any ATIS stations associated with the profile.
    /// </summary>
    public bool HasAnyStations
    {
        get => _hasAnyStations;
        set => this.RaiseAndSetIfChanged(ref _hasAnyStations, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="StatusLabel"/> should be shown.
    /// </summary>
    public bool StatusLabelVisible
    {
        get => _statusLabelVisible;
        set => this.RaiseAndSetIfChanged(ref _statusLabelVisible, value);
    }

    /// <summary>
    /// Gets or sets the label that describes the current connection status of the mini-window.
    /// </summary>
    public string? StatusLabel
    {
        get => _statusLabel;
        set => this.RaiseAndSetIfChanged(ref _statusLabel, value);
    }

    /// <summary>
    /// Gets or sets the list of all connected ATIS stations.
    /// </summary>
    public ReadOnlyObservableCollection<AtisStationViewModel> Stations
    {
        get => _stations;
        set => this.RaiseAndSetIfChanged(ref _stations, value);
    }

    /// <summary>
    /// Gets or sets the filtered list of connected ATIS stations that are visible on the mini window.
    /// </summary>
    public ReadOnlyObservableCollection<AtisStationViewModel> FilteredStations
    {
        get => _filteredStations;
        set => this.RaiseAndSetIfChanged(ref _filteredStations, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the window controls (pin and restore) buttons are visible.
    /// Only visible on mouse hover.
    /// </summary>
    public bool IsControlsVisible
    {
        get => _isControlsVisible;
        set => this.RaiseAndSetIfChanged(ref _isControlsVisible, value);
    }

    /// <summary>
    /// Initializes the connection to a shared source of <see cref="AtisStationViewModel"/>s.
    /// Binds connected stations to <see cref="Stations"/> and filters visible stations to <see cref="FilteredStations"/>.
    /// Also updates the <see cref="StatusLabel"/> and <see cref="StatusLabelVisible"/> properties based on connection and visibility status.
    /// </summary>
    /// <param name="sharedSource">The shared source list containing ATIS station view models.</param>
    public void Initialize(SourceList<AtisStationViewModel> sharedSource)
    {
        var connection = sharedSource.Connect()
            .AutoRefresh(x => x.NetworkConnectionStatus)
            .AutoRefresh(x => x.Ordinal)
            .AutoRefresh(x => x.IsVisibleOnMiniWindow)
            .Filter(filter => filter.NetworkConnectionStatus
                is NetworkConnectionStatus.Connected or NetworkConnectionStatus.Observer)
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
            .Bind(out var stations);

        // Immediately assign values from the current state (before Subscribe)
        UpdateStations(stations);

        // Now subscribe to future updates
        connection.Subscribe(_ => UpdateStations(stations)).DisposeWith(_disposables);
    }

    /// <summary>
    /// Updates the position of the given window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position needs to be updated. If null, no action is taken.</param>
    public void UpdatePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Update(window);
    }

    /// <summary>
    /// Restores the position of the specified <see cref="Window"/>.
    /// </summary>
    /// <param name="window">The window whose position is to be restored. If null, no action is taken.</param>
    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Restore(window);
    }

    /// <summary>
    /// Sets the window dialog owner.
    /// </summary>
    /// <param name="owner">The owner of the dialog.</param>
    public void SetDialogOwner(IDialogOwner owner)
    {
        _dialogOwner = owner;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
        InvokeMainWindowCommand.Dispose();
        EndClientSessionCommand.Dispose();
        GC.SuppressFinalize(this);
    }

    private void UpdateStations(ReadOnlyObservableCollection<AtisStationViewModel> readonlyStations)
    {
        Stations = readonlyStations;
        HasAnyStations = readonlyStations.Any();

        var filteredOnlineStations = readonlyStations
            .Where(x => x.IsVisibleOnMiniWindow)
            .ToList();

        _filteredStationsSource.Clear();
        foreach (var station in filteredOnlineStations)
        {
            _filteredStationsSource.Add(station);
        }

        FilteredStations = _filteredStations;

        if (!readonlyStations.Any())
        {
            StatusLabel = "NO ATISES ONLINE";
            StatusLabelVisible = true;
        }
        else if (!filteredOnlineStations.Any())
        {
            StatusLabel = "NO FILTERED ATISES";
            StatusLabelVisible = true;
        }
        else
        {
            StatusLabel = string.Empty;
            StatusLabelVisible = false;
        }
    }

    private void InvokeMainWindow(ICloseable window)
    {
        window.Close();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow?.Show();
        }
    }

    private async Task<bool> HandleEndClientSession()
    {
        if (_dialogOwner == null)
            return true;

        if (Stations.Any(x => x.NetworkConnectionStatus == NetworkConnectionStatus.Connected))
        {
            if (await MessageBox.ShowDialog((Window)_dialogOwner,
                    "You still have active ATIS connections. Are you sure you want to exit?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxIcon.Warning, centerWindow: true) == MessageBoxResult.No)
            {
                return false;
            }
        }

        _sessionManager.EndSession();
        return true;
    }
}
