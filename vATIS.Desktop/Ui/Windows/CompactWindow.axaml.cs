// <copyright file="CompactWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
        InitializeComponent();
        ViewModel = viewModel;
        Closed += OnClosed;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactWindow"/> class.
    /// </summary>
    public CompactWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PositionChanged += OnPositionChanged;
        if (DataContext is CompactWindowViewModel model)
        {
            model.RestorePosition(this);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel?.Dispose();
    }

    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Button && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (DataContext is CompactWindowViewModel model)
        {
            model.UpdatePosition(this);
        }

        // Get the screen the window is currently on
        var currentScreen = Screens.ScreenFromPoint(Position);
        if (currentScreen == null) return;

        var screenBounds = currentScreen.WorkingArea;
        var windowLeft = Position.X;
        var windowRight = windowLeft + (int)Width;

        var isRightOffScreen = windowRight > screenBounds.X + screenBounds.Width;
        var isLeftOffScreen = windowLeft < screenBounds.X;

        // Adjust alignment based on visibility
        if (isRightOffScreen)
        {
            WindowControls.HorizontalAlignment = HorizontalAlignment.Left;
        }
        else if (isLeftOffScreen)
        {
            WindowControls.HorizontalAlignment = HorizontalAlignment.Right;
        }
        else
        {
            // Keep default alignment when fully visible
            WindowControls.HorizontalAlignment = HorizontalAlignment.Right;
        }
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.IsControlsVisible = true;
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.IsControlsVisible = false;
        }
    }
}
