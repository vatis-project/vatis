// <copyright file="TransitionLevelDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents the dialog window for configuring the transition level.
/// </summary>
public partial class TransitionLevelDialog : ReactiveWindow<TransitionLevelDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionLevelDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with this dialog.</param>
    public TransitionLevelDialog(TransitionLevelDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitionLevelDialog"/> class.
    /// </summary>
    public TransitionLevelDialog()
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
