// <copyright file="CustomTabControl.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Controls;

public class CustomTabControl : TabControl
{
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new CustomTabItem();
    }
}
