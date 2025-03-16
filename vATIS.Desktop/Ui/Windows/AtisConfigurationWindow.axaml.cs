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
    private int _currentStationIndex;
    private bool _suppressSelectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    /// <param name="viewModel">The view model to be used as the data context for this window.</param>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="AtisConfigurationWindow"/> class.
    /// </summary>
    public AtisConfigurationWindow()
    {
        InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private async void Stations_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (Stations.SelectedIndex < 0)
                return;

            if (_suppressSelectionChanged)
                return;

            if (ViewModel is not { } model)
                return;

            if (model.HasUnsavedChanges && Stations.SelectedIndex != _currentStationIndex)
            {
                var result = await MessageBox.ShowDialog(this,
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Confirm",
                    MessageBoxButton.YesNo,
                    MessageBoxIcon.Information);

                if (result == MessageBoxResult.No)
                {
                    _suppressSelectionChanged = true;
                    Stations.SelectedIndex = _currentStationIndex;
                    _suppressSelectionChanged = false;

                    e.Handled = true;
                    return;
                }

                model.HasUnsavedChanges = false;
            }

            if (e.AddedItems.Count > 0 && e.AddedItems[0] is AtisStation station)
            {
                await model.SelectedAtisStationChanged.Execute(station);
                _currentStationIndex = Stations.SelectedIndex;
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
