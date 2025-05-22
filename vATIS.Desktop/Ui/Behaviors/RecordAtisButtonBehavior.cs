// <copyright file="RecordAtisButtonBehavior.cs" company="Justin Shannon">
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
using Vatsim.Vatis.Atis;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior that enables a blinking background effect on the RECORD ATIS button.
/// </summary>
public class RecordAtisButtonBehavior : Behavior<Button>
{
    /// <summary>
    /// Identifies the <see cref="RecordedAtisState"/> property.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<RecordedAtisState> RecordedAtisStateProperty =
        AvaloniaProperty.Register<RecordAtisButtonBehavior, RecordedAtisState>(nameof(RecordedAtisState));

    /// <summary>
    /// Identifies the <see cref="BlinkOnColor"/> property.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<IBrush> BlinkOnColorProperty =
        AvaloniaProperty.Register<RecordAtisButtonBehavior, IBrush>(nameof(BlinkOnColor), Brushes.Gold);

    /// <summary>
    /// Identifies the <see cref="BlinkOffColor"/> property.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<IBrush> BlinkOffColorProperty =
        AvaloniaProperty.Register<RecordAtisButtonBehavior, IBrush>(nameof(BlinkOffColor), Brushes.Aqua);

    /// <summary>
    /// Identifies the <see cref="BlinkDuration"/> property.
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly StyledProperty<TimeSpan> BlinkDurationProperty =
        AvaloniaProperty.Register<RecordAtisButtonBehavior, TimeSpan>(nameof(BlinkDuration), TimeSpan.FromSeconds(60));

    private static readonly SolidColorBrush s_grayBrush = new(Color.FromRgb(50, 50, 50));
    private static readonly SolidColorBrush s_blueBrush = new(Color.FromRgb(0, 70, 150));
    private static readonly List<RecordAtisButtonBehavior> s_instances = [];
    private static bool s_isBlinking;
    private CancellationTokenSource? _blinkCts;

    static RecordAtisButtonBehavior()
    {
        DispatcherTimer.Run(OnTimerTick, TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Gets or sets a value indicating whether the blinking effect is active.
    /// </summary>
    public RecordedAtisState RecordedAtisState
    {
        get => GetValue(RecordedAtisStateProperty);
        set => SetValue(RecordedAtisStateProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used for the "on" blink state.
    /// </summary>
    public IBrush BlinkOnColor
    {
        get => GetValue(BlinkOnColorProperty);
        set => SetValue(BlinkOnColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the brush used for the "off" blink state.
    /// </summary>
    public IBrush BlinkOffColor
    {
        get => GetValue(BlinkOffColorProperty);
        set => SetValue(BlinkOffColorProperty, value);
    }

    /// <summary>
    /// Gets or sets the duration that the control will blink before reverting
    /// to its original foreground color.
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

        if (AssociatedObject is null)
        {
            return;
        }

        s_instances.Add(this);

        this.GetObservable(RecordedAtisStateProperty).Subscribe(state =>
        {
            if (state != RecordedAtisState.Connected)
                return;

            _blinkCts?.Cancel();
            _blinkCts = new CancellationTokenSource();
            var token = _blinkCts.Token;

            // Capture BlinkDuration from UI thread
            var blinkDuration = BlinkDuration;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(blinkDuration, token);

                    if (!token.IsCancellationRequested)
                    {
                        Dispatcher.UIThread.Post(() => s_isBlinking = false);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Ignore
                }
            }, token);
        });
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        _blinkCts?.Cancel();
        _blinkCts?.Dispose();
        _blinkCts = null;

        if (AssociatedObject is null)
        {
            return;
        }

        AssociatedObject.Background = s_grayBrush;
        s_instances.Remove(this);
    }

    private static bool OnTimerTick()
    {
        s_isBlinking = !s_isBlinking;

        foreach (var instance in s_instances)
        {
            if (instance.AssociatedObject is null)
            {
                continue;
            }

            if (instance.RecordedAtisState == RecordedAtisState.Expired)
            {
                instance.AssociatedObject.Background = s_isBlinking ? instance.BlinkOnColor : instance.BlinkOffColor;
            }
            else
            {
                instance.AssociatedObject.Background = instance.RecordedAtisState == RecordedAtisState.Connected
                    ? s_blueBrush
                    : s_grayBrush;
            }
        }

        return true;
    }
}
