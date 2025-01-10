using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Serilog;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.ViewModels.AtisConfiguration;

namespace Vatsim.Vatis.Ui.AtisConfiguration;

public partial class PresetsView : UserControl
{
    private int _previousSelectedPresetIndex;
    private bool _suppressSelectionChanged;

    public PresetsView()
    {
        this.InitializeComponent();
    }

    private void AtisTemplate_OnTextChanged(object? sender, EventArgs e)
    {
        if (this.DataContext is PresetsViewModel model)
        {
            if (!this.AtisTemplate.TextArea.IsFocused)
            {
                return;
            }

            model.HasUnsavedChanges = true;
        }
    }

    private async void SelectedPreset_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (this._suppressSelectionChanged)
            {
                return;
            }

            if (this.DataContext is PresetsViewModel model)
            {
                if (model.DialogOwner == null)
                {
                    return;
                }

                if (model.HasUnsavedChanges && this.SelectedPreset.SelectedIndex != this._previousSelectedPresetIndex)
                {
                    var result = await MessageBox.ShowDialog(
                        (Window)model.DialogOwner,
                        "You have unsaved changes. Are you sure you want to discard them?",
                        "Confirm",
                        MessageBoxButton.YesNo,
                        MessageBoxIcon.Information);

                    if (result == MessageBoxResult.No)
                    {
                        this._suppressSelectionChanged = true;
                        this.SelectedPreset.SelectedIndex = this._previousSelectedPresetIndex;
                        this._suppressSelectionChanged = false;

                        e.Handled = true;
                        return;
                    }
                }

                if (e.AddedItems.Count > 0 && e.AddedItems[0] is AtisPreset preset)
                {
                    await model.SelectedPresetChanged.Execute(preset);
                    this._previousSelectedPresetIndex = this.SelectedPreset.SelectedIndex;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while loading presets");
        }
    }
}