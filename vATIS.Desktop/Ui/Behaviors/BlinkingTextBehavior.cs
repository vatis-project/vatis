// <copyright file="BlinkingTextBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior that enables a blinking text effect on supported Avalonia controls.
/// The foreground color alternates between <see cref="BlinkOnColor"/> and <see cref="BlinkOffColor"/> while <see cref="IsBlinking"/> is true.
/// </summary>
public class BlinkingTextBehavior : Behavior<Control>
{
    /// <summary>
    /// Identifies the <see cref="IsBlinking"/> property.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<bool> IsBlinkingProperty =
        AvaloniaProperty.Register<BlinkingTextBehavior, bool>(nameof(IsBlinking));

    /// <summary>
    /// Identifies the <see cref="BlinkOnColor"/> property.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<IBrush> BlinkOnColorProperty =
        AvaloniaProperty.Register<BlinkingTextBehavior, IBrush>(nameof(BlinkOnColor),
            new SolidColorBrush(Color.FromRgb(255, 204, 1)));

    /// <summary>
    /// Identifies the <see cref="BlinkOffColor"/> property.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<IBrush> BlinkOffColorProperty =
        AvaloniaProperty.Register<BlinkingTextBehavior, IBrush>(nameof(BlinkOffColor), Brushes.Aqua);

    /// <summary>
    /// Identifies the <see cref="BlinkDuration"/> property.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<TimeSpan> BlinkDurationProperty =
        AvaloniaProperty.Register<BlinkingTextBehavior, TimeSpan>(nameof(BlinkDuration), TimeSpan.FromSeconds(30));

    private static readonly List<BlinkingTextBehavior> s_instances = [];
    private static bool s_isBlinking;
    private IBrush? _originalBrush;
    private CancellationTokenSource? _blinkCts;

    static BlinkingTextBehavior()
    {
        DispatcherTimer.Run(OnTimerTick, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Gets or sets a value indicating whether the blinking effect is active.
    /// </summary>
    public bool IsBlinking
    {
        get => GetValue(IsBlinkingProperty);
        set => SetValue(IsBlinkingProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used when the text is in the "on" blink state.
    /// </summary>
    public IBrush BlinkOnColor
    {
        get => GetValue(BlinkOnColorProperty);
        set => SetValue(BlinkOnColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used when the text is in the "off" blink state.
    /// </summary>
    public IBrush BlinkOffColor
    {
        get => GetValue(BlinkOffColorProperty);
        set => SetValue(BlinkOffColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the duration that the control will blink before reverting
    /// to its original foreground color. The default is 30 seconds.
    /// </summary>
    public TimeSpan BlinkDuration
    {
        get => GetValue(BlinkDurationProperty);
        set => SetValue(BlinkDurationProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            // Delay grabbing the original brush until it's actually set by the binding
            AssociatedObject.AttachedToVisualTree += (_, _) => _originalBrush = GetForeground(AssociatedObject);
            s_instances.Add(this);

            this.GetObservable(IsBlinkingProperty).Subscribe(isBlinking =>
            {
                if (!isBlinking)
                {
                    return;
                }

                _blinkCts?.Cancel();
                _blinkCts = new CancellationTokenSource();
                var token = _blinkCts.Token;

                var weakRef = new WeakReference<BlinkingTextBehavior>(this);
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(BlinkDuration, token);

                        if (!token.IsCancellationRequested && weakRef.TryGetTarget(out var target))
                        {
                            Dispatcher.UIThread.Post(() => target.IsBlinking = false);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore
                    }
                }, token);
            });
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        _blinkCts?.Cancel();
        _blinkCts?.Dispose();
        _blinkCts = null;

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
            if (instance.AssociatedObject is null || !instance.IsEnabled)
            {
                continue;
            }

            if (instance.IsBlinking)
            {
                SetForeground(instance.AssociatedObject, s_isBlinking ? instance.BlinkOnColor : instance.BlinkOffColor);
            }
            else
            {
                SetForeground(instance.AssociatedObject, instance._originalBrush);
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
