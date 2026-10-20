// <copyright file="DataGridOverflowToolTipBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Shows the full text of a <c>DataGrid</c> cell as a tooltip when the cell is too narrow to display it.
/// </summary>
public class DataGridOverflowToolTipBehavior : Behavior<DataGrid>
{
    private DataGridCell? _lastCell;

    /// <summary>
    /// Determines whether the text of a <see cref="TextBlock"/> is wider than the space it has been given.
    /// </summary>
    /// <param name="textBlock">The text block to check.</param>
    /// <returns>True if the text is cut off.</returns>
    public static bool IsTextTrimmed(TextBlock textBlock)
    {
        if (string.IsNullOrEmpty(textBlock.Text) || textBlock.Bounds.Width <= 0)
        {
            return false;
        }

        var formatted = new FormattedText(
            textBlock.Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
            textBlock.FontSize,
            Brushes.Black);

        return formatted.Width > textBlock.Bounds.Width + 0.5;
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject?.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        AssociatedObject?.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
        base.OnDetaching();
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var cell = (e.Source as Visual)?.FindAncestorOfType<DataGridCell>(includeSelf: true);
        if (cell == null || cell == _lastCell)
        {
            return;
        }

        _lastCell = cell;

        // The tip is set before the hover delay elapses, so it is ready when the tooltip would open.
        // The tip uses the cell's font (monospace in the Contractions grid) and wraps very long values.
        ToolTip.SetTip(
            cell,
            cell.Content is TextBlock textBlock && IsTextTrimmed(textBlock)
                ? new TextBlock
                {
                    Text = textBlock.Text,
                    FontFamily = textBlock.FontFamily,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 500,
                }
                : null);
    }
}
