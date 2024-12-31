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
        InitializeComponent();
        
        ViewModel = viewModel;
        ViewModel.Owner = this;
        Closed += OnClosed;
    }

    public StaticAirportConditionsDialog()
    {
        InitializeComponent();
    }
    
    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}