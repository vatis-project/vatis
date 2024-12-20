using ReactiveUI;
using System.Reactive;
using Vatsim.Vatis.Config;

namespace Vatsim.Vatis.Ui.ViewModels;
public class TopMostViewModel : ReactiveViewModelBase
{
    private IAppConfig? mAppConfig;

    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    private bool mIsTopMost;
    public bool IsTopMost
    {
        get => mIsTopMost;
        set => this.RaiseAndSetIfChanged(ref mIsTopMost, value);
    }

    public static TopMostViewModel Instance { get; } = new();

    private TopMostViewModel()
    {
        ToggleIsTopMost = ReactiveCommand.Create(HandleToggleIsTopMost);
    }

    public void Initialize(IAppConfig appConfig)
    {
        mAppConfig = appConfig;
        IsTopMost = mAppConfig.AlwaysOnTop;
    }

    private void HandleToggleIsTopMost()
    {
        IsTopMost = !IsTopMost;

        if (mAppConfig != null)
        {
            mAppConfig.AlwaysOnTop = IsTopMost;
            mAppConfig.SaveConfig();
        }
    }
}