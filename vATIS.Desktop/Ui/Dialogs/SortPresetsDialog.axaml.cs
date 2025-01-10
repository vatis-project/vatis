using System;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class SortPresetsDialog : ReactiveWindow<SortPresetsDialogViewModel>, ICloseable
{
    public SortPresetsDialog(SortPresetsDialogViewModel viewModel)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;
        this.Closed += this.OnClosed;
    }

    public SortPresetsDialog()
    {
        this.InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
}