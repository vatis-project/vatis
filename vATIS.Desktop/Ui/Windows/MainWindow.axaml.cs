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
        this.InitializeComponent();

        this.ViewModel = viewModel;
        this.ViewModel.Owner = this;

        this.Opened += this.OnOpened;
        this.Closed += this.OnClosed;
        this.Closing += this.OnClosing;
    }

    public MainWindow()
    {
        this.InitializeComponent();
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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.PositionChanged += this.OnPositionChanged;
        if (this.DataContext is MainWindowViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (this.DataContext is MainWindowViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}