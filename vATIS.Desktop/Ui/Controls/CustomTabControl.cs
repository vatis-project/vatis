// <copyright file="CustomTabControl.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;
using Avalonia.Input;

namespace Vatsim.Vatis.Ui.Controls;

/// <summary>
/// Represents a custom tab control that extends the functionality of <see cref="TabControl"/>.
/// </summary>
public class CustomTabControl : TabControl
{
    /// <inheritdoc/>
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);

        // Tab enters the control at the element that last had focus, which skips the tabs and the rest of the
        // content. Forget it so Tab walks through the content in order.
        KeyboardNavigation.SetTabOnceActiveElement(this, null);
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new CustomTabItem();
    }
}
