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
            state = new WindowScale(new LayoutTransformControl());
            Tracked.Add(window, state);
            Windows.Add(new WeakReference<Window>(window));
            Apply(window, state);
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
        var ratio = Scale / state.Applied;
        state.Applied = Scale;
        state.Host.LayoutTransform = new ScaleTransform(Scale, Scale);

        if (Math.Abs(ratio - 1) < 0.0001)
        {
            return;
        }

        window.Width = Multiply(window.Width, ratio);
        window.Height = Multiply(window.Height, ratio);
        window.MinWidth = Multiply(window.MinWidth, ratio);
        window.MinHeight = Multiply(window.MinHeight, ratio);
        window.MaxWidth = Multiply(window.MaxWidth, ratio);
        window.MaxHeight = Multiply(window.MaxHeight, ratio);
    }

    private static double Multiply(double value, double ratio)
    {
        return double.IsNaN(value) || double.IsInfinity(value) ? value : value * ratio;
    }

    private sealed class WindowScale(LayoutTransformControl host)
    {
        public LayoutTransformControl Host { get; } = host;

        public double Applied { get; set; } = 1;
    }
}
