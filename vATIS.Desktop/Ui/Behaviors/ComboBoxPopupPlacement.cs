// <copyright file="ComboBoxPopupPlacement.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Keeps the drop-down list the same small distance from the combo box when it opens upward. The list has more
/// room around it below than above so its shadow is not clipped, which only lines up when it opens downward.
/// </summary>
public static class ComboBoxPopupPlacement
{
    private const double Gap = 3;

    /// <summary>
    /// Starts adjusting the placement of every combo box drop-down that uses the dark style.
    /// </summary>
    public static void Register()
    {
        ComboBox.IsDropDownOpenProperty.Changed.AddClassHandler<ComboBox>(OnIsDropDownOpenChanged);
    }

    private static void OnIsDropDownOpenChanged(ComboBox comboBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (!comboBox.Classes.Contains("Dark"))
        {
            return;
        }

        var popup = comboBox.GetVisualDescendants().OfType<Popup>().FirstOrDefault();
        if (popup == null)
        {
            return;
        }

        if (e.NewValue is not true)
        {
            popup.ClearValue(Popup.VerticalOffsetProperty);
            return;
        }

        Dispatcher.UIThread.Post(() => Adjust(comboBox, popup), DispatcherPriority.Loaded);
    }

    private static void Adjust(ComboBox comboBox, Popup popup)
    {
        if (!comboBox.IsDropDownOpen || popup.Child is not Border host || host.GetVisualRoot() is not PopupRoot root)
        {
            return;
        }

        var comboTop = comboBox.PointToScreen(default).Y;
        if (root.PointToScreen(default).Y < comboTop)
        {
            popup.VerticalOffset = host.Padding.Bottom - Gap;
        }
    }
}
