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

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        ViewModel.Owner = this;
        
        Opened += OnOpened;
        Closed += OnClosed;
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Check if the window close request was triggered by the user (e.g., ALT+F4 or similar)
        if (!e.IsProgrammatic)
        {
            // Terminate the client session and navigate back to the profile dialog
            Dispatcher.UIThread.Invoke(() => ViewModel?.EndClientSessionCommand.Execute().Subscribe());
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        ViewModel?.PopulateAtisStations();
        ViewModel?.ConnectToHub();
        ViewModel?.StartWebsocket();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.DisconnectFromHub();
        ViewModel?.StopWebsocket();
        ViewModel?.Dispose();
    }

    public MainWindow()
    {
        InitializeComponent();
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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PositionChanged += OnPositionChanged;
        if (DataContext is MainWindowViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (DataContext is MainWindowViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}