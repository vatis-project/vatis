// <copyright file="TextEditorUpperCaseBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior that translates all input text into uppercase for an associated <see cref="AvaloniaEdit.TextEditor"/> instance.
/// </summary>
public class TextEditorUpperCaseBehavior : Behavior<TextEditor>
{
    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.IsEnabled)
        {
            this.AssociatedObject?.AddHandler(
                InputElement.TextInputEvent,
                this.TextInputHandler,
                RoutingStrategies.Tunnel);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (this.IsEnabled)
        {
            this.AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, this.TextInputHandler);
        }
    }

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        e.Text = e.Text?.ToUpperInvariant();
    }
}
