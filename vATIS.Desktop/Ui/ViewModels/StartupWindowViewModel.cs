using ReactiveUI;
using System;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Ui.ViewModels;

public class StartupWindowViewModel : ReactiveViewModelBase
{
    private string mStatus = "";
    public string Status
    {
        get => mStatus;
        set => this.RaiseAndSetIfChanged(ref mStatus, value);
    }

    public StartupWindowViewModel()
    {
        MessageBus.Current.Listen<StartupStatusChanged>().Subscribe(evt =>
        {
            Status = evt.Status;
        });
    }
}