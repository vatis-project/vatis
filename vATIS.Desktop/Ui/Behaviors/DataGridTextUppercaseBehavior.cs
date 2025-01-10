// <copyright file="DataGridTextUppercaseBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior for a <see cref="DataGrid"/> that transforms text input into uppercase.
/// </summary>
public class DataGridTextUppercaseBehavior : Behavior<DataGrid>
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
        e.Text = e.Text?.ToUpperInvariant();
    }
}
