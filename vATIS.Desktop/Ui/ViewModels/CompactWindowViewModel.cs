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

    public ReactiveCommand<ICloseable, Unit> InvokeMainWindowCommand { get; }

    private string _currentTime = DateTime.UtcNow.ToString("HH:mm/ss");
    public string CurrentTime
    {
        get => _currentTime;
        set => this.RaiseAndSetIfChanged(ref _currentTime, value);
    }

    private ReadOnlyObservableCollection<AtisStationViewModel> _stations = new([]);
    public ReadOnlyObservableCollection<AtisStationViewModel> Stations
    {
        get => _stations;
        set => this.RaiseAndSetIfChanged(ref _stations, value);
    }

    public CompactWindowViewModel(IWindowLocationService windowLocationService)
    {
        _windowLocationService = windowLocationService;

        DispatcherTimer timer = new()
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        timer.Tick += (_, _) => CurrentTime = DateTime.UtcNow.ToString("HH:mm/ss");
        timer.Start();

        InvokeMainWindowCommand = ReactiveCommand.Create<ICloseable>(InvokeMainWindow);
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
            return;

        _windowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        _windowLocationService.Restore(window);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InvokeMainWindowCommand.Dispose();
    }
}
