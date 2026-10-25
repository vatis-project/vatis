// <copyright file="AtisLetterKeyboard.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Input;

namespace Vatsim.Vatis.Ui.Components;

/// <summary>
/// What a key press does on the focused ATIS letter.
/// </summary>
public enum AtisLetterKeyAction
{
    /// <summary>
    /// The key does nothing.
    /// </summary>
    None,

    /// <summary>
    /// Acknowledges the update, or advances the letter, like a click.
    /// </summary>
    AcknowledgeOrIncrement,

    /// <summary>
    /// Goes back one letter, like a right click.
    /// </summary>
    Decrement,

    /// <summary>
    /// Switches to typing the letter, like a shift double click.
    /// </summary>
    EditLetter,
}

/// <summary>
/// The keyboard controls of the ATIS letter, so it can be changed without a mouse.
/// </summary>
public static class AtisLetterKeyboard
{
    /// <summary>
    /// Gets the action for a key pressed on the focused ATIS letter.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="modifiers">The modifier keys held down.</param>
    /// <param name="isObserver">True when the station is an observer, which can only acknowledge an update.</param>
    /// <returns>The action.</returns>
    public static AtisLetterKeyAction GetAction(Key key, KeyModifiers modifiers, bool isObserver)
    {
        if (modifiers != KeyModifiers.None)
        {
            return AtisLetterKeyAction.None;
        }

        return key switch
        {
            Key.Enter or Key.Return or Key.Space => AtisLetterKeyAction.AcknowledgeOrIncrement,
            Key.Up or Key.Right when !isObserver => AtisLetterKeyAction.AcknowledgeOrIncrement,
            Key.Down or Key.Left when !isObserver => AtisLetterKeyAction.Decrement,
            Key.F2 when !isObserver => AtisLetterKeyAction.EditLetter,
            _ => AtisLetterKeyAction.None,
        };
    }
}
