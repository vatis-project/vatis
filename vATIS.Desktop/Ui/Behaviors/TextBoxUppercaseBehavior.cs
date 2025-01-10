// <copyright file="TextBoxUppercaseBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior designed to ensure that all text input in a TextBox control is converted to uppercase.
/// </summary>
public class TextBoxUppercaseBehavior : Behavior<TextBox>
{
    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.IsEnabled)
        {
            this.AssociatedObject?.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Tunnel);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (this.IsEnabled)
        {
            this.AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
        }
    }

    private static void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        e.Text = e.Text?.ToUpperInvariant();
    }
}
