// <copyright file="NewContractionDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Slugify;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class NewContractionDialog : ReactiveWindow<NewContractionDialogViewModel>, ICloseable
{
    private static readonly SlugHelper s_slug = new();

    public NewContractionDialog(NewContractionDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
    }

    public NewContractionDialog()
    {
        InitializeComponent();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private void Variable_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox textBox)
        {
            textBox.Text = s_slug.GenerateSlug(textBox.Text).ToUpperInvariant().Replace("-", "_");
        }
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
