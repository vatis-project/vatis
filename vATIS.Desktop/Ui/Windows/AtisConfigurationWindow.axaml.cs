// <copyright file="AtisConfigurationWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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

/// <summary>
/// Represents the ATIS configuration window.
/// </summary>
public partial class AtisConfigurationWindow : ReactiveWindow<AtisConfigurationWindowViewModel>, ICloseable,
    IDialogOwner
{
    private int currentStationIndex;
    private bool suppressSelectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model to be used as the data context for this window.</param>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
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

            if (this.suppressSelectionChanged)
            {
                return;
            }

            if (this.ViewModel is not { } model)
            {
                return;
            }

            if (model.HasUnsavedChanges && this.Stations.SelectedIndex != this.currentStationIndex)
            {
                var result = await MessageBox.ShowDialog(
                    this,
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information);

                if (result == MessageBoxResult.No)
                {
                    this.suppressSelectionChanged = true;
                    this.Stations.SelectedIndex = this.currentStationIndex;
                    this.suppressSelectionChanged = false;

                    e.Handled = true;
                    return;
                }
            }

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is AtisStation station)
            {
                await model.SelectedAtisStationChanged.Execute(station);
                this.currentStationIndex = this.Stations.SelectedIndex;
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
