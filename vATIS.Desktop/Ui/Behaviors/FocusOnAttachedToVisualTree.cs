// <copyright file="FocusOnAttachedToVisualTree.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Represents a behavior that automatically focuses the associated <see cref="TextBox"/> control
/// when it is attached to the visual tree.
/// </summary>
public class FocusOnAttachedToVisualTree : Behavior<TextBox>
{
    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        if (AssociatedObject is { } textbox)
        {
            textbox.Focus();
        }
    }
}
