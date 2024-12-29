using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class StaticDefinitionEditorDialog : ReactiveWindow<StaticDefinitionEditorDialogViewModel>, ICloseable
{
    public StaticDefinitionEditorDialog(StaticDefinitionEditorDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }
    
    public StaticDefinitionEditorDialog()
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