using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class StaticNotamsDialog : ReactiveWindow<StaticNotamsDialogViewModel>, ICloseable
{
    public StaticNotamsDialog(StaticNotamsDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        ViewModel.Owner = this;
    }

    public StaticNotamsDialog()
    {
        InitializeComponent();
    }
}