// <copyright file="ComboBoxItemMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Common;

/// <summary>
/// Represents metadata for a combo box item, containing display text and associated value.
/// </summary>
public class ComboBoxItemMeta
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComboBoxItemMeta"/> class.
    /// </summary>
    /// <param name="display">The text to display.</param>
    /// <param name="value">The associated value.</param>
    public ComboBoxItemMeta(string display, string value)
    {
        Display = display;
        Value = value;
    }

    /// <summary>
    /// Gets or sets the text to display for the combo box item.
    /// </summary>
    public string Display { get; set; }

    /// <summary>
    /// Gets or sets the associated value for the combo box item.
    /// </summary>
    public string Value { get; set; }
}
