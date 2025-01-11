// <copyright file="RoutineObservationTimeFormatBehavior.cs" company="Justin Shannon">
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
/// Represents a behavior used to enforce a specific input format within a TextBox
/// for routine observation times, validating that input contains only numeric characters
/// and commas.
/// </summary>
public class RoutineObservationTimeFormatBehavior : Behavior<TextBox>
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

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (e.Text != null && e.Text.Any(c => !char.IsNumber(c) && c != ','))
        {
            e.Handled = true;
        }
    }
}
