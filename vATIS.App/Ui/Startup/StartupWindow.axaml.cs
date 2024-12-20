using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Startup;

public partial class StartupWindow : ReactiveWindow<StartupWindowViewModel>
{
    public StartupWindow(StartupWindowViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public StartupWindow()
    {
        InitializeComponent();
    }
}