// <copyright file="ListBoxKeyboardFocus.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Gives a <see cref="ListBox"/> a <c>:keynav</c> style class while it is used from the keyboard. A list moves focus
/// between its items itself when the arrow keys are pressed, which Avalonia does not count as keyboard focus, so the
/// focused item could not be outlined for it. The mouse clears the class again.
/// </summary>
public static class ListBoxKeyboardFocus
{
    /// <summary>
    /// Defines the Enabled attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<ListBox, ListBox, bool>("Enabled");

    static ListBoxKeyboardFocus()
    {
        EnabledProperty.Changed.AddClassHandler<ListBox>(OnEnabledChanged);
    }

    /// <summary>
    /// Gets a value indicating whether the list tracks keyboard use.
    /// </summary>
    /// <param name="listBox">The list.</param>
    /// <returns>True if tracked.</returns>
    public static bool GetEnabled(ListBox listBox)
    {
        return listBox.GetValue(EnabledProperty);
    }

    /// <summary>
    /// Sets whether the list tracks keyboard use. It is meant to be turned on from a style and not off again.
    /// </summary>
    /// <param name="listBox">The list.</param>
    /// <param name="value">True to track.</param>
    public static void SetEnabled(ListBox listBox, bool value)
    {
        listBox.SetValue(EnabledProperty, value);
    }

    private static void OnEnabledChanged(ListBox listBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        void Set(bool value) => ((IPseudoClasses)listBox.Classes).Set(":keynav", value);

        listBox.AddHandler(InputElement.KeyDownEvent, (_, _) => Set(true), RoutingStrategies.Tunnel);
        listBox.AddHandler(InputElement.PointerPressedEvent, (_, _) => Set(false), RoutingStrategies.Tunnel);
        listBox.AddHandler(
            InputElement.GotFocusEvent,
            (_, args) =>
            {
                if (args.NavigationMethod == NavigationMethod.Tab)
                {
                    Set(true);
                }
            });
    }
}
