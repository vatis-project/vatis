using Avalonia.Input;
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

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}