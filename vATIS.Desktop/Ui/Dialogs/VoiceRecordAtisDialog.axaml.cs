// <copyright file="VoiceRecordAtisDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog window for recording voice ATIS in the application.
/// </summary>
public partial class VoiceRecordAtisDialog : ReactiveWindow<VoiceRecordAtisDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceRecordAtisDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with the dialog.</param>
    public VoiceRecordAtisDialog(VoiceRecordAtisDialogViewModel viewModel)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;
        this.Closed += this.OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceRecordAtisDialog"/> class.
    /// </summary>
    public VoiceRecordAtisDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Handles the operations when the dialog is opened.
    /// </summary>
    /// <param name="e">The event arguments associated with the open operation.</param>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        this.ViewModel!.DialogOwner = this;
    }

    /// <summary>
    /// Executes custom logic when the loaded event is triggered.
    /// </summary>
    /// <param name="e">The event data for the loaded event.</param>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.PositionChanged += this.OnPositionChanged;
        if (this.DataContext is VoiceRecordAtisDialogViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        this.ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (this.DataContext is VoiceRecordAtisDialogViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}
