// <copyright file="CompactWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.ReactiveUI;
using Serilog;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Windows;

/// <summary>
/// Represents a compact window designed as part of the VATSIM user interface.
/// </summary>
public partial class CompactWindow : ReactiveWindow<CompactWindowViewModel>, ICloseable, IDialogOwner
{
    private bool _isPointerInside;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompactWindow"/> class with the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model to be used by the window.</param>
    public CompactWindow(CompactWindowViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        ViewModel.SetDialogOwner(this);

        Closed += OnClosed;
        Closing += OnClosing;
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

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            // Check if the window close request was triggered by the user (e.g., ALT+F4 or similar)
            if (ViewModel is null || e.IsProgrammatic)
                return;

            e.Cancel = true;

            var shouldClose = await ViewModel.EndClientSessionCommand.Execute().FirstAsync();
            if (shouldClose)
            {
                e.Cancel = false;
                Close();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnClosing Exception");
        }
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
            // Keep the default alignment when fully visible
            WindowControls.HorizontalAlignment = HorizontalAlignment.Right;
        }
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _isPointerInside = true;

        if (ViewModel != null && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            ViewModel.IsControlsVisible = true;
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _isPointerInside = false;

        if (ViewModel != null)
        {
            ViewModel.IsControlsVisible = false;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel != null && e.KeyModifiers.HasFlag(KeyModifiers.Control) && _isPointerInside)
        {
            ViewModel.IsControlsVisible = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.IsControlsVisible = false;
        }
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Border { Name: "Root" })
        {
            ViewModel?.InvokeMainWindowCommand.Execute(this).Subscribe();
        }
    }
}
