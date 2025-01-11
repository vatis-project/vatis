// <copyright file="CustomTabItem.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Controls;

public class CustomTabItem : TabItem
{
    private static readonly StyledProperty<string> s_atisLetterProperty =
        AvaloniaProperty.Register<CustomTabItem, string>(nameof(AtisLetter));

    public string AtisLetter
    {
        get => GetValue(s_atisLetterProperty);
        set => SetValue(s_atisLetterProperty, value);
    }

    private static readonly StyledProperty<bool> s_isConnectedProperty =
        AvaloniaProperty.Register<CustomTabItem, bool>(nameof(IsConnected));

    public bool IsConnected
    {
        get => GetValue(s_isConnectedProperty);
        set => SetValue(s_isConnectedProperty, value);
    }

    public CustomTabItem()
    {

    }
}
