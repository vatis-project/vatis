using Avalonia.Controls;
using Avalonia.Input;
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

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}