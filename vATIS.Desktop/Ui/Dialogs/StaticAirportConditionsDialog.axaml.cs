using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class StaticAirportConditionsDialog : ReactiveWindow<StaticAirportConditionsDialogViewModel>, ICloseable
{
    public StaticAirportConditionsDialog(StaticAirportConditionsDialogViewModel viewModel)
    {
        this.InitializeComponent();

        this.ViewModel = viewModel;
        this.ViewModel.Owner = this;
        this.Closed += this.OnClosed;
    }

    public StaticAirportConditionsDialog()
    {
        this.InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
}