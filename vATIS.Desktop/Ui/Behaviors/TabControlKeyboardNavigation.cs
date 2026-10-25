// <copyright file="TabControlKeyboardNavigation.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Makes Tab walk through the content of a <see cref="TabControl"/> in order. Tab enters a tab control at the element
/// that last had focus inside it, so after the first pass the tabs and the rest of the content could not be reached.
/// </summary>
public static class TabControlKeyboardNavigation
{
    /// <summary>
    /// Defines the Enabled attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<TabControl, TabControl, bool>("Enabled");

    static TabControlKeyboardNavigation()
    {
        EnabledProperty.Changed.AddClassHandler<TabControl>(OnEnabledChanged);
    }

    /// <summary>
    /// Gets a value indicating whether the tab control forgets where focus was.
    /// </summary>
    /// <param name="tabControl">The tab control.</param>
    /// <returns>True if enabled.</returns>
    public static bool GetEnabled(TabControl tabControl)
    {
        return tabControl.GetValue(EnabledProperty);
    }

    /// <summary>
    /// Sets whether the tab control forgets where focus was. It is meant to be turned on from a style and not off again.
    /// </summary>
    /// <param name="tabControl">The tab control.</param>
    /// <param name="value">True to enable.</param>
    public static void SetEnabled(TabControl tabControl, bool value)
    {
        tabControl.SetValue(EnabledProperty, value);
    }

    private static void OnEnabledChanged(TabControl tabControl, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        tabControl.GotFocus += (_, _) => KeyboardNavigation.SetTabOnceActiveElement(tabControl, null);
    }
}
