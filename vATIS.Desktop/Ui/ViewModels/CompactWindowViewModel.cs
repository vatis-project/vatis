// <copyright file="CompactWindowViewModel.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using Vatsim.Vatis.Ui.Services;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the compact window in the UI.
/// </summary>
public class CompactWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IWindowLocationService _windowLocationService;
    private ReadOnlyObservableCollection<AtisStationViewModel> _stations = new([]);
    private bool _isControlsVisible;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactWindowViewModel"/> class.
    /// </summary>
    /// <param name="windowLocationService">The service responsible for managing window locations.</param>
    public CompactWindowViewModel(IWindowLocationService windowLocationService)
    {
        _windowLocationService = windowLocationService;

        InvokeMainWindowCommand = ReactiveCommand.Create<ICloseable>(InvokeMainWindow);
    }

    /// <summary>
    /// Gets the command used to invoke the main window logic.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> InvokeMainWindowCommand { get; }

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

    /// <inheritdoc />
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InvokeMainWindowCommand.Dispose();
    }

    private void InvokeMainWindow(ICloseable window)
    {
        window.Close();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow?.Show();
        }
    }
}
