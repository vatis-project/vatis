using System.Reactive;
using ReactiveUI;
using Vatsim.Vatis.Config;

namespace Vatsim.Vatis.Ui.ViewModels;

public class CompactWindowTopMostViewModel : ReactiveViewModelBase
{
    private IAppConfig? mAppConfig;

    public ReactiveCommand<Unit, Unit> ToggleIsTopMost { get; private set; }

    private bool mIsTopMost;
    public bool IsTopMost
    {
        get => mIsTopMost;
        set => this.RaiseAndSetIfChanged(ref mIsTopMost, value);
    }

    public static CompactWindowTopMostViewModel Instance { get; } = new();

    private CompactWindowTopMostViewModel()
    {
        ToggleIsTopMost = ReactiveCommand.Create(HandleToggleIsTopMost);
    }

    public void Initialize(IAppConfig appConfig)
    {
        mAppConfig = appConfig;
        IsTopMost = mAppConfig.CompactWindowAlwaysOnTop;
    }

    private void HandleToggleIsTopMost()
    {
        IsTopMost = !IsTopMost;

        if (mAppConfig != null)
        {
            mAppConfig.CompactWindowAlwaysOnTop = IsTopMost;
            mAppConfig.SaveConfig();
        }
    }
}