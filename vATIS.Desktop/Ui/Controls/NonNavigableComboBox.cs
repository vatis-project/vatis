// <copyright file="NonNavigableComboBox.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Vatsim.Vatis.Ui.Controls;

/// <summary>
/// Represents a customized ComboBox that disables navigation using the up and down arrow keys and mouse wheel.
/// </summary>
public class NonNavigableComboBox : ComboBox
{
    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ComboBox);

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Disable up/down arrow navigation while closed, so the selection is not changed by accident. Once the list
        // is open the arrows must work to choose an item, and Alt+Down opens it.
        if (e.Key is Key.Up or Key.Down && !IsDropDownOpen && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }
}
