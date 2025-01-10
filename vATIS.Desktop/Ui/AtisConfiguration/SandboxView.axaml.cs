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
        this.InitializeComponent();
    }

    private void AirportConditions_OnTextChanged(object? sender, EventArgs e)
    {
        if (this.DataContext is SandboxViewModel vm)
        {
            if (vm.SelectedPreset == null)
            {
                return;
            }

            if (this._airportConditionsInitialized)
            {
                if (!this.AirportConditions.TextArea.IsFocused)
                {
                    return;
                }

                vm.HasUnsavedAirportConditions = true;
            }

            this._airportConditionsInitialized = true;
        }
    }

    private void NotamFreeText_OnTextChanged(object? sender, EventArgs e)
    {
        if (this.DataContext is SandboxViewModel vm)
        {
            if (vm.SelectedPreset == null)
            {
                return;
            }

            if (this._notamsInitialized)
            {
                if (!this.NotamFreeText.TextArea.IsFocused)
                {
                    return;
                }

                vm.HasUnsavedNotams = true;
            }

            this._notamsInitialized = true;
        }
    }
}