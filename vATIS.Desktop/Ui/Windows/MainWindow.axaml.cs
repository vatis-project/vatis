// <copyright file="MainWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Windows;

/// <summary>
/// Represents the main window of the application, providing the primary UI and functionality for user interaction.
/// </summary>
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with the main window.</param>
    public MainWindow(MainWindowViewModel viewModel)
    {
        this.InitializeComponent();

        this.ViewModel = viewModel;
        this.ViewModel.Owner = this;

        this.Opened += this.OnOpened;
        this.Closed += this.OnClosed;
        this.Closing += this.OnClosing;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Called when the window has finished loading.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.PositionChanged += this.OnPositionChanged;
        if (this.DataContext is MainWindowViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Check if the window close request was triggered by the user (e.g., ALT+F4 or similar)
        if (!e.IsProgrammatic)
        {
            // Terminate the client session and navigate back to the profile dialog
            Dispatcher.UIThread.Invoke(() => this.ViewModel?.EndClientSessionCommand.Execute().Subscribe());
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        this.ViewModel?.PopulateAtisStations();
        this.ViewModel?.ConnectToHub();
        this.ViewModel?.StartWebsocket();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.ViewModel?.DisconnectFromHub();
        this.ViewModel?.StopWebsocket();
        this.ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void OnMinimizeWindow(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (this.DataContext is MainWindowViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}
