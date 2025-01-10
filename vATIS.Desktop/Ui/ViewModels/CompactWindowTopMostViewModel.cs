using System;
using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Config;

namespace Vatsim.Vatis.Ui.ViewModels;

public class CompactWindowTopMostViewModel : ReactiveViewModelBase
{
    private static readonly Lazy<CompactWindowTopMostViewModel> s_instance = new(
        () =>
            new CompactWindowTopMostViewModel());

    private IAppConfig? _appConfig;

    private bool _isTopMost;

    private CompactWindowTopMostViewModel()
    {
        this.ToggleIsTopMost = ReactiveCommand.Create(this.HandleToggleIsTopMost);
    }

    public static CompactWindowTopMostViewModel Instance => s_instance.Value;

    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    public bool IsTopMost
    {
        get => this._isTopMost;
        set => this.RaiseAndSetIfChanged(ref this._isTopMost, value);
    }

    public void Initialize(IAppConfig appConfig)
    {
        this._appConfig = appConfig;
        this.IsTopMost = this._appConfig.CompactWindowAlwaysOnTop;
    }

    private void HandleToggleIsTopMost()
    {
        this.IsTopMost = !this.IsTopMost;

        if (this._appConfig != null)
        {
            this._appConfig.CompactWindowAlwaysOnTop = this.IsTopMost;
            this._appConfig.SaveConfig();
        }
    }
}