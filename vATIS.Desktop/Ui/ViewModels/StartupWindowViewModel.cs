using ReactiveUI;
using System;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Ui.ViewModels;

public class StartupWindowViewModel : ReactiveViewModelBase
{
    private string _status = "";
    public string Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    public StartupWindowViewModel()
    {
        MessageBus.Current.Listen<StartupStatusChanged>().Subscribe(evt =>
        {
            Status = evt.Status;
        });
    }
}
