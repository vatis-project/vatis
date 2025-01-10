// <copyright file="TextBoxAlphaOnlyBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior that restricts input in an associated <see cref="TextBox"/> to alphabetic characters only.
/// </summary>
public class TextBoxAlphaOnlyBehavior : Behavior<TextBox>
{
    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject?.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        this.AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
        base.OnDetaching();
    }

    private static void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (e.Text != null && e.Text.Any(c => !char.IsLetter(c)))
        {
            e.Handled = true;
        }
    }
}
