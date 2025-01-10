// <copyright file="CompactWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Windows;

/// <summary>
/// Represents a compact window designed as part of the VATSIM user interface.
/// </summary>
public partial class CompactWindow : ReactiveWindow<CompactWindowViewModel>, ICloseable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompactWindow"/> class with the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model to be used by the window.</param>
    public CompactWindow(CompactWindowViewModel viewModel)
    {
        this.InitializeComponent();
        this.ViewModel = viewModel;
        this.Closed += this.OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactWindow"/> class.
    /// </summary>
    public CompactWindow()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Called when the window is loaded.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.PositionChanged += this.OnPositionChanged;
        if (this.DataContext is CompactWindowViewModel model)
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
        if (e.Source is Border or TextBlock && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (this.DataContext is CompactWindowViewModel model)
        {
            model.UpdatePosition(this);
        }
    }
}
