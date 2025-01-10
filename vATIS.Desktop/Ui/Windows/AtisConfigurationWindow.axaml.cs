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

public partial class AtisConfigurationWindow : ReactiveWindow<AtisConfigurationWindowViewModel>, ICloseable,
    IDialogOwner
{
    private int _currentStationIndex;
    private bool _suppressSelectionChanged;

    public AtisConfigurationWindow(AtisConfigurationWindowViewModel viewModel)
    {
        this.InitializeComponent();
        this.DataContext = viewModel;
        this.ViewModel?.Initialize(this);
        this.Closed += this.OnClosed;

        this.WhenAnyValue(x => x.ViewModel!.SelectedAtisStation).Subscribe(
            station =>
            {
                var index = this.Stations.Items.IndexOf(station);
                this.Stations.SelectedIndex = index;
            });
    }

    public AtisConfigurationWindow()
    {
        this.InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.ViewModel?.Dispose();
    }

    private async void Stations_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (this.Stations.SelectedIndex < 0)
            {
                return;
            }

            if (this._suppressSelectionChanged)
            {
                return;
            }

            if (this.ViewModel is not { } model)
            {
                return;
            }

            if (model.HasUnsavedChanges && this.Stations.SelectedIndex != this._currentStationIndex)
            {
                var result = await MessageBox.ShowDialog(
                    this,
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information);

                if (result == MessageBoxResult.No)
                {
                    this._suppressSelectionChanged = true;
                    this.Stations.SelectedIndex = this._currentStationIndex;
                    this._suppressSelectionChanged = false;

                    e.Handled = true;
                    return;
                }
            }

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is AtisStation station)
            {
                await model.SelectedAtisStationChanged.Execute(station);
                this._currentStationIndex = this.Stations.SelectedIndex;
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
            this.BeginMoveDrag(e);
        }
    }
}