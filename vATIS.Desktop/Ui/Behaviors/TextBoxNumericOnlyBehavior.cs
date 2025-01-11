// <copyright file="TextBoxNumericOnlyBehavior.cs" company="Justin Shannon">
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
/// Restricts input in a <see cref="TextBox"/> to numeric characters only.
/// </summary>
public class TextBoxNumericOnlyBehavior : Behavior<TextBox>
{
    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject?.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
        base.OnDetaching();
    }

    private static void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (sender == null)
        {
            return;
        }

        if (e.Text != null && e.Text.Any(c => !char.IsNumber(c)))
        {
            e.Handled = true;
        }
    }
}
