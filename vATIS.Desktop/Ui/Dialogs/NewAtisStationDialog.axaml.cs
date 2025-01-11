// <copyright file="NewAtisStationDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog for creating or configuring a new ATIS station.
/// </summary>
public partial class NewAtisStationDialog : ReactiveWindow<NewAtisStationDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewAtisStationDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with this dialog.</param>
    public NewAtisStationDialog(NewAtisStationDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NewAtisStationDialog"/> class.
    /// </summary>
    public NewAtisStationDialog()
    {
        InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
