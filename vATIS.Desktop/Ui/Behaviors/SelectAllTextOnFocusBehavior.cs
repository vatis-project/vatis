// <copyright file="SelectAllTextOnFocusBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class SelectAllTextOnFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        AssociatedObject?.AddHandler(InputElement.GotFocusEvent, OnGotFocusEvent, Avalonia.Interactivity.RoutingStrategies.Bubble);
    }

    protected override void OnDetachedFromVisualTree()
    {
        base.OnDetachedFromVisualTree();
        AssociatedObject?.RemoveHandler(InputElement.GotFocusEvent, OnGotFocusEvent);
    }

    private void OnGotFocusEvent(object? sender, GotFocusEventArgs e)
    {
        AssociatedObject?.SelectAll();
    }
}
