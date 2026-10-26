// <copyright file="SortAtisStationsDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog for sorting ATIS stations.
/// </summary>
public partial class SortAtisStationsDialog : ReactiveWindow<SortAtisStationsDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SortAtisStationsDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with this dialog.</param>
    public SortAtisStationsDialog(SortAtisStationsDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        KeyDown += OnWindowKeyDown;
        Closed += OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SortAtisStationsDialog"/> class.
    /// </summary>
    public SortAtisStationsDialog()
    {
        InitializeComponent();
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        KeyDown -= OnWindowKeyDown;
        ViewModel?.Dispose();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
