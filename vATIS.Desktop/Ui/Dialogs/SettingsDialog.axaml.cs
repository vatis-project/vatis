using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class SettingsDialog : ReactiveWindow<SettingsDialogViewModel>, ICloseable
{
    public SettingsDialog(SettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public SettingsDialog()
    {
        InitializeComponent();
    }

    private void CancelButtonClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }
}