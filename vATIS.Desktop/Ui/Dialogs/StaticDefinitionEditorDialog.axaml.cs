// <copyright file="StaticDefinitionEditorDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

public partial class StaticDefinitionEditorDialog : ReactiveWindow<StaticDefinitionEditorDialogViewModel>, ICloseable
{
    public StaticDefinitionEditorDialog(StaticDefinitionEditorDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
    }

    public StaticDefinitionEditorDialog()
    {
        InitializeComponent();
    }
    
    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}