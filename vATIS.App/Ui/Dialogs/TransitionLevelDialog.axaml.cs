using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class TransitionLevelDialog : ReactiveWindow<TransitionLevelDialogViewModel>, ICloseable
{
    public TransitionLevelDialog(TransitionLevelDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public TransitionLevelDialog()
    {
        InitializeComponent();
    }
}