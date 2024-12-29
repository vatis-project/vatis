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
    private int mPreviousSelectedPresetIndex;
    private bool mSuppressSelectionChanged;

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
            if (mSuppressSelectionChanged)
                return;

            if (DataContext is PresetsViewModel model)
            {
                if (model.DialogOwner == null)
                    return;

                if (model.HasUnsavedChanges && SelectedPreset.SelectedIndex != mPreviousSelectedPresetIndex)
                {
                    var result = await MessageBox.ShowDialog((Window)model.DialogOwner,
                        "You have unsaved changes. Are you sure you want to discard them?",
                        "Confirm",
                        MessageBoxButton.YesNo,
                        MessageBoxIcon.Information);

                    if (result == MessageBoxResult.No)
                    {
                        mSuppressSelectionChanged = true;
                        SelectedPreset.SelectedIndex = mPreviousSelectedPresetIndex;
                        mSuppressSelectionChanged = false;

                        e.Handled = true;
                        return;
                    }
                }

                if (e.AddedItems.Count > 0 && e.AddedItems[0] is AtisPreset preset)
                {
                    await model.SelectedPresetChanged.Execute(preset);
                    mPreviousSelectedPresetIndex = SelectedPreset.SelectedIndex;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while loading presets");
        }
    }
}