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
using Avalonia.Threading;
using ReactiveUI;
using Vatsim.Vatis.Ui.Services;

namespace Vatsim.Vatis.Ui.ViewModels;

/// <summary>
/// Represents the view model for the compact window in the UI.
/// </summary>
public class CompactWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IWindowLocationService windowLocationService;
    private string currentTime = DateTime.UtcNow.ToString("HH:mm/ss");
    private ReadOnlyObservableCollection<AtisStationViewModel> stations = new([]);

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactWindowViewModel"/> class.
    /// </summary>
    /// <param name="windowLocationService">The service responsible for managing window locations.</param>
    public CompactWindowViewModel(IWindowLocationService windowLocationService)
    {
        this.windowLocationService = windowLocationService;

        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        timer.Tick += (_, _) => this.CurrentTime = DateTime.UtcNow.ToString("HH:mm/ss");
        timer.Start();

        this.InvokeMainWindowCommand = ReactiveCommand.Create<ICloseable>(this.InvokeMainWindow);
    }

    /// <summary>
    /// Gets the command used to invoke the main window logic.
    /// </summary>
    public ReactiveCommand<ICloseable, Unit> InvokeMainWindowCommand { get; }

    /// <summary>
    /// Gets or sets the current time in coordinated universal time (UTC) formatted as "HH:mm/ss".
    /// </summary>
    public string CurrentTime
    {
        get => this.currentTime;
        set => this.RaiseAndSetIfChanged(ref this.currentTime, value);
    }

    /// <summary>
    /// Gets or sets the collection of ATIS station view models displayed in the compact window.
    /// </summary>
    public ReadOnlyObservableCollection<AtisStationViewModel> Stations
    {
        get => this.stations;
        set => this.RaiseAndSetIfChanged(ref this.stations, value);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.InvokeMainWindowCommand.Dispose();
    }

    /// <summary>
    /// Updates the position of the given window using the window location service.
    /// </summary>
    /// <param name="window">The window whose position needs to be updated. If null, no action is taken.</param>
    public void UpdatePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this.windowLocationService.Update(window);
    }

    /// <summary>
    /// Restores the position of the specified <see cref="Window"/>.
    /// </summary>
    /// <param name="window">The window whose position is to be restored. If null, no action is taken.</param>
    public void RestorePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this.windowLocationService.Restore(window);
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
