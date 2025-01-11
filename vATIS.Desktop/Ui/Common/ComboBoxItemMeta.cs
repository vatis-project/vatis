// <copyright file="ComboBoxItemMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Common;
public class ComboBoxItemMeta
{
    public string Display { get; set; }
    public string Value { get; set; }

    public ComboBoxItemMeta(string display, string value)
    {
        Display = display;
        Value = value;
    }
}