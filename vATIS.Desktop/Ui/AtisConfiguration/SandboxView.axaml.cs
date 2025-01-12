// <copyright file="SandboxView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

/// <summary>
/// Represents a view for the sandbox section used within the ATIS configuration UI.
/// </summary>
public partial class SandboxView : UserControl
{
    private bool _airportConditionsInitialized;
    private bool _notamsInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="SandboxView"/> class.
    /// </summary>
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
