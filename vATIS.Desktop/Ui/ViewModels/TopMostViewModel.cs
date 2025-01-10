using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Config;

namespace Vatsim.Vatis.Ui.ViewModels;

public class TopMostViewModel : ReactiveViewModelBase
{
    private IAppConfig? _appConfig;

    private bool _isTopMost;

    private TopMostViewModel()
    {
        this.ToggleIsTopMost = ReactiveCommand.Create(this.HandleToggleIsTopMost);
    }

    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    public bool IsTopMost
    {
        get => this._isTopMost;
        set => this.RaiseAndSetIfChanged(ref this._isTopMost, value);
    }

    public static TopMostViewModel Instance { get; } = new();

    public void Initialize(IAppConfig appConfig)
    {
        this._appConfig = appConfig;
        this.IsTopMost = this._appConfig.AlwaysOnTop;
    }

    private void HandleToggleIsTopMost()
    {
        this.IsTopMost = !this.IsTopMost;

        if (this._appConfig != null)
        {
            this._appConfig.AlwaysOnTop = this.IsTopMost;
            this._appConfig.SaveConfig();
        }
    }
}