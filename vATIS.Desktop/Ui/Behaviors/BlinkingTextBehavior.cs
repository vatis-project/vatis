using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class BlinkingTextBehavior : Behavior<Control>
{
    private IBrush? mOriginalBrush;
    private static bool _isBlinking;
    private static readonly List<BlinkingTextBehavior> Instances = [];

    public static readonly StyledProperty<bool> IsBlinkingProperty =
        AvaloniaProperty.Register<BlinkingTextBehavior, bool>(nameof(IsBlinking));

    public bool IsBlinking
    {
        get => GetValue(IsBlinkingProperty);
        set => SetValue(IsBlinkingProperty, value);
    }

    public static readonly StyledProperty<IBrush> Color1Property =
        AvaloniaProperty.Register<BlinkingTextBehavior, IBrush>(nameof(Color1), Brushes.Aqua);

    public IBrush Color1
    {
        get => GetValue(Color1Property);
        set => SetValue(Color1Property, value);
    }

    public static readonly StyledProperty<IBrush> Color2Property =
        AvaloniaProperty.Register<BlinkingTextBehavior, IBrush>(nameof(Color2),
            new SolidColorBrush(Color.FromRgb(255, 204, 1)));

    public IBrush Color2
    {
        get => GetValue(Color2Property);
        set => SetValue(Color2Property, value);
    }

    static BlinkingTextBehavior()
    {
        DispatcherTimer.Run(OnTimerTick, TimeSpan.FromMilliseconds(500));
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not null)
        {
            mOriginalBrush = GetForeground(AssociatedObject);
            Instances.Add(this);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject is not null)
        {
            SetForeground(AssociatedObject, mOriginalBrush);
            Instances.Remove(this);
        }
    }

    private static bool OnTimerTick()
    {
        _isBlinking = !_isBlinking;

        foreach (var instance in Instances)
        {
            if (instance.AssociatedObject is not null)
            {
                if (instance.IsBlinking)
                {
                    SetForeground(instance.AssociatedObject, _isBlinking ? instance.Color1 : instance.Color2);
                }
                else
                {
                    SetForeground(instance.AssociatedObject, instance.mOriginalBrush);
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