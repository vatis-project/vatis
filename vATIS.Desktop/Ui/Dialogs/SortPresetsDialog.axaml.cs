using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class SortPresetsDialog : ReactiveWindow<SortPresetsDialogViewModel>, ICloseable
{
    public SortPresetsDialog(SortPresetsDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    public SortPresetsDialog()
    {
        InitializeComponent();
    }
}