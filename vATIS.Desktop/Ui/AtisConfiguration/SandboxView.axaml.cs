using System;
using Avalonia.Controls;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

public partial class SandboxView : UserControl
{
    private bool _airportConditionsInitialized;
    private bool _notamsInitialized;

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

            if (_airportConditionsInitialized)
            {
                if (!AirportConditions.TextArea.IsFocused)
                    return;

                vm.HasUnsavedAirportConditions = true;
            }

            _airportConditionsInitialized = true;
        }
    }

    private void NotamFreeText_OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SandboxViewModel vm)
        {
            if (vm.SelectedPreset == null)
                return;

            if (_notamsInitialized)
            {
                if (!NotamFreeText.TextArea.IsFocused)
                    return;

                vm.HasUnsavedNotams = true;
            }

            _notamsInitialized = true;
        }
    }
}
