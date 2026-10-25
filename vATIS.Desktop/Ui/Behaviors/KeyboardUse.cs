// <copyright file="KeyboardUse.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Gives a control a <c>:keynav</c> style class while it is used from the keyboard, so a style can show focus for the
/// keyboard but not for the mouse. A key press sets it, and a mouse press clears it.
/// </summary>
public static class KeyboardUse
{
    /// <summary>
    /// Defines the Track attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> TrackProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, bool>("Track");

    static KeyboardUse()
    {
        TrackProperty.Changed.AddClassHandler<Control>(OnTrackChanged);
    }

    /// <summary>
    /// Gets a value indicating whether the control tracks keyboard use.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <returns>True if tracked.</returns>
    public static bool GetTrack(Control control)
    {
        return control.GetValue(TrackProperty);
    }

    /// <summary>
    /// Sets whether the control tracks keyboard use. It is meant to be turned on from a style and not off again.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="value">True to track.</param>
    public static void SetTrack(Control control, bool value)
    {
        control.SetValue(TrackProperty, value);
    }

    private static void OnTrackChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        void Set(bool value) => ((IPseudoClasses)control.Classes).Set(":keynav", value);

        control.AddHandler(InputElement.KeyDownEvent, (_, _) => Set(true), RoutingStrategies.Tunnel);
        control.AddHandler(InputElement.PointerPressedEvent, (_, _) => Set(false), RoutingStrategies.Tunnel);
        control.GotFocus += (_, args) =>
        {
            if (args.NavigationMethod == NavigationMethod.Tab)
            {
                Set(true);
            }
        };
    }
}
