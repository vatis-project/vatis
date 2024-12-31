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
    private readonly IWindowLocationService mWindowLocationService;

    public ReactiveCommand<ICloseable, Unit> InvokeMainWindowCommand { get; private set; }

    private string mCurrentTime = DateTime.UtcNow.ToString("HH:mm/ss");
    public string CurrentTime
    {
        get => mCurrentTime;
        set => this.RaiseAndSetIfChanged(ref mCurrentTime, value);
    }

    private ReadOnlyObservableCollection<AtisStationViewModel> mStations = new([]);
    public ReadOnlyObservableCollection<AtisStationViewModel> Stations
    {
        get => mStations;
        set => this.RaiseAndSetIfChanged(ref mStations, value);
    }

    public CompactWindowViewModel(IWindowLocationService windowLocationService)
    {
        mWindowLocationService = windowLocationService;

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

        mWindowLocationService.Update(window);
    }

    public void RestorePosition(Window? window)
    {
        if (window == null)
            return;

        mWindowLocationService.Restore(window);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InvokeMainWindowCommand.Dispose();
    }
}
