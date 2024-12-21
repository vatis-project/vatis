using System;
using Avalonia.Controls;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

public partial class SandboxView : UserControl
{
    private bool mAirportConditionsInitialized;
    private bool mNotamsInitialized;

    public SandboxView()
    {
        InitializeComponent();
    }

    private void AirportConditions_OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SandboxViewModel vm)
        {
            if (vm.SelectedPreset == null)
                return;

            if (mAirportConditionsInitialized)
            {
                if (!AirportConditions.TextArea.IsFocused)
                    return;
                
                vm.HasUnsavedAirportConditions = true;
            }

            mAirportConditionsInitialized = true;
        }
    }

    private void NotamFreeText_OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SandboxViewModel vm)
        {
            if (vm.SelectedPreset == null)
                return;
            
            if (mNotamsInitialized)
            {
                if (!NotamFreeText.TextArea.IsFocused)
                    return;
                
                vm.HasUnsavedNotams = true;
            }

            mNotamsInitialized = true;
        }
    }
}