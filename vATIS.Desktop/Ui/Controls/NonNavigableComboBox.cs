// <copyright file="NonNavigableComboBox.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Vatsim.Vatis.Ui.Controls;

/// <summary>
/// Represents a customized ComboBox that disables navigation using the up and down arrow keys.
/// </summary>
public class NonNavigableComboBox : ComboBox
{
    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ComboBox);

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down)
        {
            // Disable up/down arrow navigation
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }
}
