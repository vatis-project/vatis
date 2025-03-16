// <copyright file="PresetsView.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Serilog;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

/// <summary>
/// Represents a view for configuring presets in the ATIS configuration system.
/// </summary>
public partial class PresetsView : UserControl
{
    private int _previousSelectedPresetIndex;
    private bool _suppressSelectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PresetsView"/> class.
    /// </summary>
    public PresetsView()
    {
        InitializeComponent();
    }

    private void AtisTemplate_OnTextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PresetsViewModel model)
        {
            if (!AtisTemplate.TextArea.IsFocused)
                return;

            model.HasUnsavedChanges = true;
        }
    }

    private async void SelectedPreset_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_suppressSelectionChanged)
                return;

            if (DataContext is PresetsViewModel model)
            {
                if (model.DialogOwner == null)
                    return;

                if (model.HasUnsavedChanges && SelectedPreset.SelectedIndex != _previousSelectedPresetIndex)
                {
                    var result = await MessageBox.ShowDialog((Window)model.DialogOwner,
                        "You have unsaved changes. Are you sure you want to discard them?",
                        "Confirm",
                        MessageBoxButton.YesNo,
                        MessageBoxIcon.Information);

                    if (result == MessageBoxResult.No)
                    {
                        _suppressSelectionChanged = true;
                        SelectedPreset.SelectedIndex = _previousSelectedPresetIndex;
                        _suppressSelectionChanged = false;

                        e.Handled = true;
                        return;
                    }

                    model.HasUnsavedChanges = false;
                }

                if (e.AddedItems.Count > 0 && e.AddedItems[0] is AtisPreset preset)
                {
                    await model.SelectedPresetChanged.Execute(preset);
                    _previousSelectedPresetIndex = SelectedPreset.SelectedIndex;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while loading presets");
        }
    }
}
