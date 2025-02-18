// <copyright file="MainWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Serilog;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Windows;

/// <summary>
/// Represents the main window of the application, providing the primary UI and functionality for user interaction.
/// </summary>
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private bool _initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with the main window.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        ViewModel.Owner = this;

        Opened += OnOpened;
        Closed += OnClosed;
        Closing += OnClosing;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PositionChanged += OnPositionChanged;
        if (DataContext is MainWindowViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            if (e.IsProgrammatic)
            {
                return;
            }

            e.Cancel = true; // Prevent the window from closing immediately

            if (ViewModel?.EndClientSessionCommand != null)
            {
                var shouldClose = await ViewModel.EndClientSessionCommand.Execute().FirstAsync();
                if (shouldClose)
                {
                    e.Cancel = false; // Allow the window to close
                }
            }
            else
            {
                e.Cancel = false; // No command available, allow closing
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnClosing Error");
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (_initialized)
            return;

        ViewModel?.PopulateAtisStations();
        ViewModel?.ConnectToHub();
        ViewModel?.StartWebsocket();

        _initialized = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.DisconnectFromHub();
        ViewModel?.StopWebsocket();
        ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnMinimizeWindow(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (DataContext is MainWindowViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}
