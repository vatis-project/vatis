using Avalonia.Controls;
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
}