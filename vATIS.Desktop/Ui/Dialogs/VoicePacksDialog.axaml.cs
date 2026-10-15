// <copyright file="VoicePacksDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents the window for managing vATIS's library of voice packs.
/// </summary>
public partial class VoicePacksDialog : ReactiveWindow<VoicePacksDialogViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VoicePacksDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with this dialog.</param>
    public VoicePacksDialog(VoicePacksDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        viewModel.Owner = this;
        Opened += async (_, _) => await viewModel.LoadAsync();
        Closed += OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VoicePacksDialog"/> class.
    /// </summary>
    public VoicePacksDialog()
    {
        InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
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
