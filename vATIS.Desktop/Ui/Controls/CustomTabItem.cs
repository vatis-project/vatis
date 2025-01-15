// <copyright file="CustomTabItem.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Controls;

/// <summary>
/// Represents a custom tab item with extended properties.
/// </summary>
public class CustomTabItem : TabItem
{
    private static readonly StyledProperty<string> s_atisLetterProperty =
        AvaloniaProperty.Register<CustomTabItem, string>(nameof(AtisLetter));

    private static readonly StyledProperty<bool> s_isConnectedProperty =
        AvaloniaProperty.Register<CustomTabItem, bool>(nameof(IsConnected));

    /// <summary>
    /// Gets or sets the ATIS letter to display in the tab item.
    /// </summary>
    public string AtisLetter
    {
        get => GetValue(s_atisLetterProperty);
        set => SetValue(s_atisLetterProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the station is connected.
    /// </summary>
    public bool IsConnected
    {
        get => GetValue(s_isConnectedProperty);
        set => SetValue(s_isConnectedProperty, value);
    }
}
