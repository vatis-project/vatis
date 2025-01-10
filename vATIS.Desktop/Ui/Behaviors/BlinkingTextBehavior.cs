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
    private static bool s_isBlinking;
    private static readonly List<BlinkingTextBehavior> s_instances = [];

    public static readonly StyledProperty<bool> IsBlinkingProperty =
        AvaloniaProperty.Register<BlinkingTextBehavior, bool>(nameof(IsBlinking));

    public static readonly StyledProperty<IBrush> Color1Property =
        AvaloniaProperty.Register<BlinkingTextBehavior, IBrush>(nameof(Color1), Brushes.Aqua);

    public static readonly StyledProperty<IBrush> Color2Property =
        AvaloniaProperty.Register<BlinkingTextBehavior, IBrush>(
            nameof(Color2),
            new SolidColorBrush(Color.FromRgb(255, 204, 1)));

    private IBrush? _originalBrush;

    static BlinkingTextBehavior()
    {
        DispatcherTimer.Run(OnTimerTick, TimeSpan.FromMilliseconds(500));
    }

    public bool IsBlinking
    {
        get => this.GetValue(IsBlinkingProperty);
        set => this.SetValue(IsBlinkingProperty, value);
    }

    public IBrush Color1
    {
        get => this.GetValue(Color1Property);
        set => this.SetValue(Color1Property, value);
    }

    public IBrush Color2
    {
        get => this.GetValue(Color2Property);
        set => this.SetValue(Color2Property, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.AssociatedObject is not null)
        {
            this._originalBrush = GetForeground(this.AssociatedObject);
            s_instances.Add(this);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (this.AssociatedObject is not null)
        {
            SetForeground(this.AssociatedObject, this._originalBrush);
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

    private static IBrush? GetForeground(Control control)
    {
        return control switch
        {
            Button button => button.Foreground,
            TextBlock textBlock => textBlock.Foreground,
            _ => throw new ArgumentException("Control must be a TextBlock or Button.")
        };
    }

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