using System;
using Avalonia.Controls;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Components;

public partial class AtisStationView : UserControl
{
    private bool mAirportConditionsInitialized;
    private bool mNotamsInitialized;
    
    public AtisStationView()
    {
        InitializeComponent();
    }

    private void AirportConditions_OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is AtisStationViewModel vm)
        {
            if (vm.SelectedAtisPreset == null)
                return;

            if (!AirportConditions.TextArea.IsFocused)
                return;

            if (mAirportConditionsInitialized)
            {
                vm.HasUnsavedAirportConditions = true;
            }

            mAirportConditionsInitialized = true;
        }
    }
    
    private void NotamFreeText_OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is AtisStationViewModel vm)
        {
            if (vm.SelectedAtisPreset == null)
                return;

            if (!NotamFreeText.TextArea.IsFocused)
                return;
            
            if (mNotamsInitialized)
            {
                vm.HasUnsavedNotams = true;
            }

            mNotamsInitialized = true;
        }
    }
}