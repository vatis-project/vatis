using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class UserInputDialog : ReactiveWindow<UserInputDialogViewModel>, ICloseable
{
    public UserInputDialog(UserInputDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public UserInputDialog()
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