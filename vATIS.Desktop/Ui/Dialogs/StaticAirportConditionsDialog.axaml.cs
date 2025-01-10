// <copyright file="StaticAirportConditionsDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog for displaying and managing static airport conditions.
/// </summary>
public partial class StaticAirportConditionsDialog : ReactiveWindow<StaticAirportConditionsDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StaticAirportConditionsDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with this dialog.</param>
    public StaticAirportConditionsDialog(StaticAirportConditionsDialogViewModel viewModel)
    {
        this.InitializeComponent();

        this.ViewModel = viewModel;
        this.ViewModel.Owner = this;
        this.Closed += this.OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticAirportConditionsDialog"/> class.
    /// </summary>
    public StaticAirportConditionsDialog()
    {
        this.InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }
}
