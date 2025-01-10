using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Startup;

public partial class StartupWindow : ReactiveWindow<StartupWindowViewModel>
{
    public StartupWindow(StartupWindowViewModel viewModel)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;
    }

    public StartupWindow()
    {
        this.InitializeComponent();
    }
}