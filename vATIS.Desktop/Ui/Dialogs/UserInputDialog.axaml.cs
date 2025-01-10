using System;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class UserInputDialog : ReactiveWindow<UserInputDialogViewModel>, ICloseable
{
    public UserInputDialog(UserInputDialogViewModel viewModel)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;
        this.Closed += this.OnClosed;
    }

    public UserInputDialog()
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