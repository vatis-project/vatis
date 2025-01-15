// <copyright file="BlinkingTextBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Provides a behavior that alternates the foreground color of a control
/// between two specified colors, creating a blinking effect.
/// </summary>
public class BlinkingTextBehavior : Behavior<Control>
{
    /// <summary>
    /// Identifies the <see cref="IsBlinkingProperty"/> dependency property, determining whether
    /// the associated control should have its text alternate between two colors, creating a blinking effect.
    /// </summary>
    public static readonly StyledProperty<bool> IsBlinkingProperty =
        AvaloniaProperty.Register<BlinkingTextBehavior, bool>(nameof(IsBlinking));

    /// <summary>
    /// Identifies the <see cref="Color1Property"/> dependency property, specifying
    /// the first foreground color used by the associated control when creating a blinking effect.
    /// </summary>
    public static readonly StyledProperty<IBrush> Color1Property =
        AvaloniaProperty.Register<BlinkingTextBehavior, IBrush>(nameof(Color1), Brushes.Aqua);

    /// <summary>
    /// Identifies the <see cref="Color2Property"/> dependency property, specifying
    /// the second foreground color used by the associated control when creating a blinking effect.
    /// </summary>
    public static readonly StyledProperty<IBrush> Color2Property =
        AvaloniaProperty.Register<BlinkingTextBehavior, IBrush>(nameof(Color2),
            new SolidColorBrush(Color.FromRgb(255, 204, 1)));

    private static readonly List<BlinkingTextBehavior> s_instances = [];
    private static bool s_isBlinking;
    private IBrush? _originalBrush;

    static BlinkingTextBehavior()
    {
        DispatcherTimer.Run(OnTimerTick, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Gets or sets a value indicating whether the associated control
    /// should alternate its text color between two specified colors, creating a blinking effect.
    /// </summary>
    public bool IsBlinking
    {
        get => GetValue(IsBlinkingProperty);
        set => SetValue(IsBlinkingProperty, value);
    }

    /// <summary>
    /// Gets or sets the first foreground color used by the associated control
    /// when creating a blinking effect.
    /// </summary>
    public IBrush Color1
    {
        get => GetValue(Color1Property);
        set => SetValue(Color1Property, value);
    }

    /// <summary>
    /// Gets or sets the second foreground color used by the associated control
    /// when creating a blinking effect.
    /// </summary>
    public IBrush Color2
    {
        get => GetValue(Color2Property);
        set => SetValue(Color2Property, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            _originalBrush = GetForeground(AssociatedObject);
            s_instances.Add(this);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is not null)
        {
            SetForeground(AssociatedObject, _originalBrush);
            s_instances.Remove(this);
        }
    }

    private static bool OnTimerTick()
    {
        s_isBlinking = !s_isBlinking;

        foreach (var instance in s_instances)
        {
            if (instance.AssociatedObject is not null)
            {
                if (instance.IsBlinking)
                {
                    SetForeground(instance.AssociatedObject, s_isBlinking ? instance.Color1 : instance.Color2);
                }
                else
                {
                    SetForeground(instance.AssociatedObject, instance._originalBrush);
                }
            }
        }

        return true;
    }

    private static IBrush? GetForeground(Control control) =>
        control switch
        {
            Button button => button.Foreground,
            TextBlock textBlock => textBlock.Foreground,
            _ => throw new ArgumentException("Control must be a TextBlock or Button.")
        };

    private static void SetForeground(Control control, IBrush? brush)
    {
        switch (control)
        {
            case Button button:
                button.Foreground = brush;
                break;
            case TextBlock textBlock:
                textBlock.Foreground = brush;
                break;
            default:
                throw new ArgumentException("Control must be a TextBlock or Button.");
        }
    }
}
