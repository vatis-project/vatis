using System;
using ReactiveUI;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Ui.ViewModels;

public class StartupWindowViewModel : ReactiveViewModelBase
{
    private string _status = "";

    public StartupWindowViewModel()
    {
        MessageBus.Current.Listen<StartupStatusChanged>().Subscribe(evt => { this.Status = evt.Status; });
    }

    public string Status
    {
        get => this._status;
        set => this.RaiseAndSetIfChanged(ref this._status, value);
    }
}