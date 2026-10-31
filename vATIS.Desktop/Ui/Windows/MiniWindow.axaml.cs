// <copyright file="MiniWindow.axaml.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using Serilog;
using Vatsim.Vatis.Ui.ViewModels;

namespace Vatsim.Vatis.Ui.Windows;

/// <summary>
/// Represents the mini-window for displaying ATIS information in a compact view.
/// </summary>
public partial class MiniWindow : ReactiveWindow<MiniWindowViewModel>, ICloseable, IDialogOwner
{
    private double _lastWidth;

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniWindow"/> class with the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model to be used by the window.</param>
    public MiniWindow(MiniWindowViewModel viewModel)
    {
        InitializeComponent();

        ViewModel = viewModel;
        ViewModel.SetDialogOwner(this);

        Closed += OnClosed;
        Closing += OnClosing;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MiniWindow"/> class.
    /// </summary>
    public MiniWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        PositionChanged += OnPositionChanged;

        ViewModel?.RestorePosition(this);
        _lastWidth = Bounds.Width;
        SizeChanged += OnSizeChanged;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        Closing -= OnClosing;
        PositionChanged -= OnPositionChanged;
        SizeChanged -= OnSizeChanged;

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

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var newWidth = e.NewSize.Width;
        var oldWidth = _lastWidth;
        _lastWidth = newWidth;

        // Ignore the first layout, the position is restored separately.
        if (oldWidth <= 0)
            return;

        var screen = Screens.ScreenFromWindow(this);
        if (screen is null)
            return;

        // Position and screen bounds are in device pixels, except on macOS where they are in points.
        var scaling = OperatingSystem.IsMacOS() ? 1.0 : RenderScaling;
        var x = Position.X;
        var y = Position.Y;

        // The window sizes to its content and grows from its left edge. When it sits on the right
        // half of the screen, shift it so the right edge stays put and it grows leftward instead.
        if (Math.Abs(newWidth - oldWidth) >= 0.5)
        {
            var centerX = x + (oldWidth * scaling / 2);
            var screenCenterX = screen.Bounds.X + (screen.Bounds.Width / 2.0);
            if (centerX > screenCenterX)
            {
                x -= (int)Math.Round((newWidth - oldWidth) * scaling);
            }
        }

        // Safety net: keep the window inside the screen's working area (excludes taskbar/menu bar).
        var area = screen.WorkingArea;
        var width = (int)Math.Round(e.NewSize.Width * scaling);
        var height = (int)Math.Round(e.NewSize.Height * scaling);
        x = Math.Max(area.X, Math.Min(x, area.Right - width));
        y = Math.Max(area.Y, Math.Min(y, area.Bottom - height));

        if (x != Position.X || y != Position.Y)
        {
            Position = new PixelPoint(x, y);
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        ViewModel?.UpdatePosition(this);
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Border { Name: "Root" })
        {
            ViewModel?.InvokeMainWindowCommand.Execute(this).Subscribe();
        }
    }
}
