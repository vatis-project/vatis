// <copyright file="PointerPressedEventTrigger.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class PointerPressedEventTrigger : Trigger<Interactive>
{
    public static readonly StyledProperty<ICommand> LeftClickCommandProperty =
        AvaloniaProperty.Register<PointerPressedEventTrigger, ICommand>(nameof(LeftClickCommand));

    public static readonly StyledProperty<ICommand> RightClickCommandProperty =
        AvaloniaProperty.Register<PointerPressedEventTrigger, ICommand>(nameof(RightClickCommand));

    public ICommand LeftClickCommand
    {
        get => GetValue(LeftClickCommandProperty);
        set => SetValue(LeftClickCommandProperty, value);
    }

    public ICommand RightClickCommand
    {
        get => GetValue(RightClickCommandProperty);
        set => SetValue(RightClickCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject?.AddHandler(InputElement.PointerPressedEvent, Handler, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject?.RemoveHandler(InputElement.PointerPressedEvent, Handler);
    }

    private void Handler(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(null);
        if (point.Properties.IsLeftButtonPressed)
        {
            LeftClickCommand.Execute(null);
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            RightClickCommand.Execute(null);
        }
    }
}
