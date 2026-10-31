// <copyright file="StaticDefinitionEditorDialog.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Dialogs;

/// <summary>
/// Represents a dialog for editing static definitions.
/// </summary>
public partial class StaticDefinitionEditorDialog : ReactiveWindow<StaticDefinitionEditorDialogViewModel>, ICloseable
{
    private double _lastHeight;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticDefinitionEditorDialog"/> class.
    /// </summary>
    /// <param name="viewModel">The view model associated with this dialog.</param>
    public StaticDefinitionEditorDialog(StaticDefinitionEditorDialogViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticDefinitionEditorDialog"/> class.
    /// </summary>
    public StaticDefinitionEditorDialog()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SizeChanged += OnSizeChanged;
    }

    // The window sizes to its content, so growing text boxes push it down. Shift it up by half the change to stay centered.
    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var previous = _lastHeight;
        _lastHeight = e.NewSize.Height;

        // The first size is the one the window was centered with.
        if (previous == 0 || Math.Abs(_lastHeight - previous) < 0.5)
        {
            return;
        }

        Position = new PixelPoint(Position.X, Position.Y - (int)Math.Round((_lastHeight - previous) * RenderScaling / 2));
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
