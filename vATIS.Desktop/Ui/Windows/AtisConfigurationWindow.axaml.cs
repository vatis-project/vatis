using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ReactiveUI;
using Serilog;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Ui.Dialogs.MessageBox;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Windows;

public partial class AtisConfigurationWindow : ReactiveWindow<AtisConfigurationWindowViewModel>, ICloseable, IDialogOwner
{
    private int mCurrentStationIndex;
    private bool mSuppressSelectionChanged;

    public AtisConfigurationWindow(AtisConfigurationWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        ViewModel?.Initialize(this);
        Closed += OnClosed;

        this.WhenAnyValue(x => x.ViewModel!.SelectedAtisStation).Subscribe(station =>
        {
            var index = Stations.Items.IndexOf(station);
            Stations.SelectedIndex = index;
        });
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    public AtisConfigurationWindow()
    {
        InitializeComponent();
    }

    private async void Stations_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (Stations.SelectedIndex < 0)
                return;
            
            if (mSuppressSelectionChanged) 
                return;
            
            if (ViewModel is not { } model) 
                return;
            
            if (model.HasUnsavedChanges && Stations.SelectedIndex != mCurrentStationIndex)
            {
                var result = await MessageBox.ShowDialog(this,
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information);

                if (result == MessageBoxResult.No)
                {
                    mSuppressSelectionChanged = true;
                    Stations.SelectedIndex = mCurrentStationIndex;
                    mSuppressSelectionChanged = false;

                    e.Handled = true;
                    return;
                }
            }

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is AtisStation station)
            {
                await model.SelectedAtisStationChanged.Execute(station);
                mCurrentStationIndex = Stations.SelectedIndex;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while loading composites");
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border { Name: "TitleBar" } && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}