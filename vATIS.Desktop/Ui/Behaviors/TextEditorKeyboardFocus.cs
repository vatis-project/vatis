// <copyright file="TextEditorKeyboardFocus.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Keyboard handling shared by every <see cref="TextEditor"/>. Tab moves to the next control instead of inserting a
/// tab, which would trap keyboard focus in the editor. The editor also gets a <c>:keyfocus</c> style class while it has
/// focus from the keyboard, so it can be highlighted the way a text box is. Focus lands on the editor's inner text
/// area, so the editor never gets the framework's own <c>:focus-visible</c>.
/// </summary>
public static class TextEditorKeyboardFocus
{
    /// <summary>
    /// Defines the Enabled attached property.
    /// </summary>
    public static readonly AttachedProperty<bool> EnabledProperty =
        AvaloniaProperty.RegisterAttached<TextEditor, TextEditor, bool>("Enabled");

    static TextEditorKeyboardFocus()
    {
        EnabledProperty.Changed.AddClassHandler<TextEditor>(OnEnabledChanged);
    }

    /// <summary>
    /// Gets a value indicating whether the editor tracks keyboard focus.
    /// </summary>
    /// <param name="editor">The editor.</param>
    /// <returns>True if tracked.</returns>
    public static bool GetEnabled(TextEditor editor)
    {
        return editor.GetValue(EnabledProperty);
    }

    /// <summary>
    /// Sets whether the editor tracks keyboard focus. It is meant to be turned on from a style and not off again.
    /// </summary>
    /// <param name="editor">The editor.</param>
    /// <param name="value">True to track.</param>
    public static void SetEnabled(TextEditor editor, bool value)
    {
        editor.SetValue(EnabledProperty, value);
    }

    private static void OnEnabledChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        editor.Options.AcceptsTab = false;
        editor.TextArea.GotFocus += (_, args) =>
            ((IPseudoClasses)editor.Classes).Set(":keyfocus", args.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional);
        editor.TextArea.LostFocus += (_, _) => ((IPseudoClasses)editor.Classes).Set(":keyfocus", false);
    }
}
