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

public class CompactWindowViewModel : ReactiveViewModelBase, IDisposable
{
    private readonly IWindowLocationService _windowLocationService;

    private string _currentTime = DateTime.UtcNow.ToString("HH:mm/ss");

    private ReadOnlyObservableCollection<AtisStationViewModel> _stations = new([]);

    public CompactWindowViewModel(IWindowLocationService windowLocationService)
    {
        this._windowLocationService = windowLocationService;

        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        timer.Tick += (_, _) => this.CurrentTime = DateTime.UtcNow.ToString("HH:mm/ss");
        timer.Start();

        this.InvokeMainWindowCommand = ReactiveCommand.Create<ICloseable>(this.InvokeMainWindow);
    }

    public ReactiveCommand<ICloseable, Unit> InvokeMainWindowCommand { get; }

    public string CurrentTime
    {
        get => this._currentTime;
        set => this.RaiseAndSetIfChanged(ref this._currentTime, value);
    }

    public ReadOnlyObservableCollection<AtisStationViewModel> Stations
    {
        get => this._stations;
        set => this.RaiseAndSetIfChanged(ref this._stations, value);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.InvokeMainWindowCommand.Dispose();
    }

    private void InvokeMainWindow(ICloseable window)
    {
        window.Close();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow?.Show();
        }
    }

    public void UpdatePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this._windowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
        {
            return;
        }

        this._windowLocationService.Restore(window);
    }
}