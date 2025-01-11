// <copyright file="SettingsDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog window for updating user settings in the application.
/// </summary>
public partial class SettingsDialog : ReactiveWindow<SettingsDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with the dialog.</param>
    public SettingsDialog(SettingsDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsDialog"/> class.
    /// </summary>
    public SettingsDialog()
    {
        InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private void CancelButtonClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
