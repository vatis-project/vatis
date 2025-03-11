// <copyright file="CompactWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Sessions;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.Services;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the compact window in the UI.
/// </summary>
public class CompactWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly IWindowLocationService _windowLocationService;
    private IDialogOwner? _dialogOwner;
    private ReadOnlyObservableCollection<AtisStationViewModel> _stations = new([]);
    private bool _isControlsVisible;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactWindowViewModel"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager service.</param>
    /// <param name="windowLocationService">The service responsible for managing window locations.</param>
    public CompactWindowViewModel(ISessionManager sessionManager, IWindowLocationService windowLocationService)
    {
        _sessionManager = sessionManager;
        _windowLocationService = windowLocationService;

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
    /// Gets or sets the collection of ATIS station view models displayed in the compact window.
    /// </summary>
    public ReadOnlyObservableCollection<AtisStationViewModel> Stations
    {
        get => _stations;
        set => this.RaiseAndSetIfChanged(ref _stations, value);
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
        GC.SuppressFinalize(this);
        InvokeMainWindowCommand.Dispose();
        EndClientSessionCommand.Dispose();
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
                    MessageBoxButton.YesNo, MessageBoxIcon.Warning) == MessageBoxResult.No)
            {
                return false;
            }
        }

        _sessionManager.EndSession();
        return true;
    }
}
