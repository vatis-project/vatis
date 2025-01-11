// <copyright file="SelectAllTextOnFocusBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior that selects all text in a <see cref="TextBox"/> when it receives focus.
/// </summary>
public class SelectAllTextOnFocusBehavior : Behavior<TextBox>
{
    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        AssociatedObject?.AddHandler(InputElement.GotFocusEvent, OnGotFocusEvent, RoutingStrategies.Bubble);
    }

    /// <inheritdoc/>
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
