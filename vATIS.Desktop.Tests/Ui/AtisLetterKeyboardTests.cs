// <copyright file="AtisLetterKeyboardTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Input;
using Vatsim.Vatis.Ui.Components;
using Xunit;

namespace Vatsim.Vatis.Tests.Ui;

public class AtisLetterKeyboardTests
{
    [Theory]
    [InlineData(Key.Enter, AtisLetterKeyAction.AcknowledgeOrIncrement)]
    [InlineData(Key.Return, AtisLetterKeyAction.AcknowledgeOrIncrement)]
    [InlineData(Key.Space, AtisLetterKeyAction.AcknowledgeOrIncrement)]
    [InlineData(Key.Up, AtisLetterKeyAction.AcknowledgeOrIncrement)]
    [InlineData(Key.Right, AtisLetterKeyAction.AcknowledgeOrIncrement)]
    [InlineData(Key.Down, AtisLetterKeyAction.Decrement)]
    [InlineData(Key.Left, AtisLetterKeyAction.Decrement)]
    [InlineData(Key.F2, AtisLetterKeyAction.EditLetter)]
    [InlineData(Key.A, AtisLetterKeyAction.None)]
    [InlineData(Key.Tab, AtisLetterKeyAction.None)]
    [InlineData(Key.Escape, AtisLetterKeyAction.None)]
    public void Connected_KeysMatchTheMouse(Key key, AtisLetterKeyAction expected)
    {
        Assert.Equal(expected, AtisLetterKeyboard.GetAction(key, KeyModifiers.None, isObserver: false));
    }

    [Theory]
    [InlineData(Key.Enter, AtisLetterKeyAction.AcknowledgeOrIncrement)]
    [InlineData(Key.Space, AtisLetterKeyAction.AcknowledgeOrIncrement)]
    [InlineData(Key.Up, AtisLetterKeyAction.None)]
    [InlineData(Key.Right, AtisLetterKeyAction.None)]
    [InlineData(Key.Down, AtisLetterKeyAction.None)]
    [InlineData(Key.Left, AtisLetterKeyAction.None)]
    [InlineData(Key.F2, AtisLetterKeyAction.None)]
    public void Observer_CanOnlyAcknowledge(Key key, AtisLetterKeyAction expected)
    {
        Assert.Equal(expected, AtisLetterKeyboard.GetAction(key, KeyModifiers.None, isObserver: true));
    }

    [Theory]
    [InlineData(KeyModifiers.Shift)]
    [InlineData(KeyModifiers.Control)]
    [InlineData(KeyModifiers.Alt)]
    [InlineData(KeyModifiers.Meta)]
    public void KeysWithModifiers_DoNothing(KeyModifiers modifiers)
    {
        Assert.Equal(AtisLetterKeyAction.None, AtisLetterKeyboard.GetAction(Key.Enter, modifiers, isObserver: false));
        Assert.Equal(AtisLetterKeyAction.None, AtisLetterKeyboard.GetAction(Key.Up, modifiers, isObserver: false));
    }
}
