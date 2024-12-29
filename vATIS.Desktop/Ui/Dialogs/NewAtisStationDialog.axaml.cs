using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class NewAtisStationDialog : ReactiveWindow<NewAtisStationDialogViewModel>, ICloseable
{
    public NewAtisStationDialog(NewAtisStationDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public NewAtisStationDialog()
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