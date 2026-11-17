// <copyright file="UiScaling.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;

namespace Vatsim.Vatis.Ui.Services;

/// <summary>
/// Scales the whole UI. Every window's content is wrapped in a scale transform and the window's fixed size is
/// multiplied to match, so changing the scale applies to open windows right away.
/// </summary>
public static class UiScaling
{
    /// <summary>
    /// The smallest scale, as a percentage.
    /// </summary>
    public const int MinPercent = 100;

    /// <summary>
    /// The largest scale, as a percentage.
    /// </summary>
    public const int MaxPercent = 300;

    /// <summary>
    /// The difference between each scale offered in the settings.
    /// </summary>
    public const int StepPercent = 25;

    private static readonly ConditionalWeakTable<Window, WindowScale> Tracked = new();
    private static readonly List<WeakReference<Window>> Windows = [];

    /// <summary>
    /// Gets the current scale, where 1 is 100%.
    /// </summary>
    public static double Scale { get; private set; } = 1;

    /// <summary>
    /// Keeps a percentage within the supported range.
    /// </summary>
    /// <param name="percent">The percentage to clamp.</param>
    /// <returns>The percentage between <see cref="MinPercent"/> and <see cref="MaxPercent"/>.</returns>
    public static int ClampPercent(int percent) => Math.Clamp(percent, MinPercent, MaxPercent);

    /// <summary>
    /// Starts scaling every window that gets content from now on. Call once at startup.
    /// </summary>
    public static void Register()
    {
        ContentControl.ContentProperty.Changed.AddClassHandler<Window>(OnWindowContentChanged);
    }

    /// <summary>
    /// Sets the scale and applies it to every open window.
    /// </summary>
    /// <param name="percent">The scale as a percentage.</param>
    public static void SetPercent(int percent)
    {
        Scale = ClampPercent(percent) / 100.0;

        for (var i = Windows.Count - 1; i >= 0; i--)
        {
            if (Windows[i].TryGetTarget(out var window) && Tracked.TryGetValue(window, out var state))
            {
                Apply(window, state);
            }
            else
            {
                Windows.RemoveAt(i);
            }
        }
    }

    private static void OnWindowContentChanged(Window window, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not Control content)
        {
            return;
        }

        if (!Tracked.TryGetValue(window, out var state))
        {
            // The window sizes from its XAML are still unscaled at this point.
            state = new WindowScale(new LayoutTransformControl(), window.MaxWidth, window.MaxHeight);
            Tracked.Add(window, state);
            Windows.Add(new WeakReference<Window>(window));
            Apply(window, state);

            // The screen the window is on is only known once it opens, and its content can be measured by then.
            window.Opened += (_, _) => Apply(window, state);
        }

        if (ReferenceEquals(content, state.Host))
        {
            return;
        }

        // Put the new content inside the scaled host instead of directly in the window.
        window.Content = state.Host;
        state.Host.Child = content;
    }

    private static void Apply(Window window, WindowScale state)
    {
        var area = GetWorkingArea(window);
        var scale = GetEffectiveScale(window, state, area);
        var ratio = scale / state.Applied;
        state.Applied = scale;
        state.Host.LayoutTransform = new ScaleTransform(scale, scale);

        if (Math.Abs(ratio - 1) >= 0.0001)
        {
            window.Width = Multiply(window.Width, ratio);
            window.Height = Multiply(window.Height, ratio);
            window.MinWidth = Multiply(window.MinWidth, ratio);
            window.MinHeight = Multiply(window.MinHeight, ratio);
        }

        // Windows that size to their content can still grow after this, so never let them pass the screen.
        window.MaxWidth = Math.Min(state.BaseMaxWidth * scale, area?.Width ?? double.PositiveInfinity);
        window.MaxHeight = Math.Min(state.BaseMaxHeight * scale, area?.Height ?? double.PositiveInfinity);

        Dispatcher.UIThread.Post(() => KeepOnScreen(window), DispatcherPriority.Background);
    }

    /// <summary>
    /// Gets the largest scale the window can use and still fit on the screen it is on.
    /// </summary>
    private static double GetEffectiveScale(Window window, WindowScale state, Size? area)
    {
        if (area is not { } available)
        {
            return Scale;
        }

        // Sizes of the window at 100%.
        var width = Unscaled(window.Width, state.Applied);
        var height = Unscaled(window.Height, state.Applied);

        if ((double.IsNaN(width) || double.IsNaN(height)) && state.Host.Child is { } content)
        {
            content.Measure(new Size(
                double.IsNaN(width) ? double.PositiveInfinity : width,
                double.IsNaN(height) ? double.PositiveInfinity : height));
            width = double.IsNaN(width) ? Math.Min(content.DesiredSize.Width, state.BaseMaxWidth) : width;
            height = double.IsNaN(height) ? Math.Min(content.DesiredSize.Height, state.BaseMaxHeight) : height;
        }

        var scale = Scale;
        if (width > 0 && !double.IsNaN(width) && !double.IsInfinity(width))
        {
            scale = Math.Min(scale, available.Width / width);
        }

        if (height > 0 && !double.IsNaN(height) && !double.IsInfinity(height))
        {
            scale = Math.Min(scale, available.Height / height);
        }

        // A window that is already bigger than the screen at 100% is left alone.
        return Math.Max(1, scale);
    }

    private static Size? GetWorkingArea(Window window)
    {
        try
        {
            var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary;
            if (screen == null || screen.Scaling <= 0)
            {
                return null;
            }

            // Leave room for the title bar and borders when the window has them.
            var chrome = window.FrameSize is { } frame ? frame - window.ClientSize : default;
            return new Size(
                (screen.WorkingArea.Width / screen.Scaling) - chrome.Width,
                (screen.WorkingArea.Height / screen.Scaling) - chrome.Height);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static void KeepOnScreen(Window window)
    {
        if (!window.IsVisible)
        {
            return;
        }

        var screen = window.Screens.ScreenFromWindow(window) ?? window.Screens.Primary;
        if (screen == null)
        {
            return;
        }

        var area = screen.WorkingArea;
        var size = window.FrameSize ?? window.ClientSize;
        var width = (int)Math.Round(size.Width * window.RenderScaling);
        var height = (int)Math.Round(size.Height * window.RenderScaling);
        var x = Math.Clamp(window.Position.X, area.X, Math.Max(area.X, area.Right - width));
        var y = Math.Clamp(window.Position.Y, area.Y, Math.Max(area.Y, area.Bottom - height));

        if (x != window.Position.X || y != window.Position.Y)
        {
            window.Position = new PixelPoint(x, y);
        }
    }

    private static double Unscaled(double value, double applied)
    {
        return double.IsNaN(value) || double.IsInfinity(value) ? double.NaN : value / applied;
    }

    private static double Multiply(double value, double ratio)
    {
        return double.IsNaN(value) || double.IsInfinity(value) ? value : value * ratio;
    }

    private sealed class WindowScale(LayoutTransformControl host, double baseMaxWidth, double baseMaxHeight)
    {
        public LayoutTransformControl Host { get; } = host;

        public double BaseMaxWidth { get; } = baseMaxWidth;

        public double BaseMaxHeight { get; } = baseMaxHeight;

        public double Applied { get; set; } = 1;
    }
}
