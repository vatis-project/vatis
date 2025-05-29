// <copyright file="ScrollToSelectedItemBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Custom;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Behavior that automatically scrolls a <see cref="TreeDataGrid"/> to ensure the selected item is visible.
/// </summary>
public class ScrollToSelectedItemBehavior : AttachedToVisualTreeBehavior<TreeDataGrid>
{
    /// <inheritdoc />
    protected override IDisposable OnAttachedToVisualTreeOverride()
    {
        if (AssociatedObject?.RowSelection == null)
        {
            return Disposable.Empty;
        }

        var rowSelection = AssociatedObject.RowSelection;
        var subscription = new SelectionChangedSubscription(this);
        rowSelection.SelectionChanged += subscription.OnSelectionChanged;
        return Disposable.Create(() => rowSelection.SelectionChanged -= subscription.OnSelectionChanged);
    }

    private void HandleSelectionChanged()
    {
        var rowSelection = AssociatedObject?.RowSelection;
        if (rowSelection == null || AssociatedObject?.Rows == null)
        {
            return;
        }

        var selectedIndexPath = rowSelection.SelectedIndex.FirstOrDefault();
        var rowIndex = AssociatedObject.Rows.ModelIndexToRowIndex(selectedIndexPath);

        // Correct the index with the index of child item, in the case when the selected item is a child.
        if (rowSelection.SelectedIndex.Count > 1)
        {
            // Skip 1 because the first index is the parent.
            // Every other index is the child index.
            rowIndex += rowSelection.SelectedIndex.Skip(1).Sum();

            // Need to add 1 to get the correct index.
            rowIndex += 1;
        }

        ScrollToItemIndex(rowIndex);
    }

    private void ScrollToItemIndex(int index)
    {
        AssociatedObject?.RowsPresenter?.BringIntoView(index);
    }

    private class SelectionChangedSubscription
    {
        private readonly ScrollToSelectedItemBehavior _behavior;

        public SelectionChangedSubscription(ScrollToSelectedItemBehavior behavior)
        {
            _behavior = behavior;
        }

        public void OnSelectionChanged(object? sender, EventArgs e)
        {
            _behavior.HandleSelectionChanged();
        }
    }
}
