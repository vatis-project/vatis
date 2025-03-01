// <copyright file="ScrollToSelectedItemBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Custom;
using ReactiveUI;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Behavior that automatically scrolls a <see cref="TreeDataGrid"/> to ensure the selected item is visible.
/// </summary>
public class ScrollToSelectedItemBehavior : AttachedToVisualTreeBehavior<TreeDataGrid>
{
    private IDisposable? _selectionChangedSubscription;

    /// <summary>
    /// Attaches to the visual tree and subscribes to the selection change event
    /// to scroll the selected item into view.
    /// </summary>
    /// <param name="disposable">A composite disposable to manage subscriptions.</param>
    protected override void OnAttachedToVisualTree(CompositeDisposable disposable)
    {
        // Ensure the associated object is a TreeDataGrid and its RowSelection is available
        if (AssociatedObject is { RowSelection: not null } treeDataGrid)
        {
            // Subscribe to the SelectionChanged event manually to avoid reflection
            _selectionChangedSubscription = treeDataGrid.RowSelection.WhenAnyValue(x => x.SelectedIndex)
                .Subscribe(_ => OnSelectionChanged(treeDataGrid));
            disposable.Add(_selectionChangedSubscription);
        }
    }

    /// <inheritdoc />
    protected override void OnDetaching()
    {
        // Unsubscribe from the event when detached
        _selectionChangedSubscription?.Dispose();
        base.OnDetaching();
    }

    /// <summary>
    /// Handles the selection change event to update the scroll position.
    /// </summary>
    private void OnSelectionChanged(TreeDataGrid treeDataGrid)
    {
        // Retrieve the first selected index directly.
        var selectedIndexPath = treeDataGrid.RowSelection?.SelectedIndex.Count > 0
            ? treeDataGrid.RowSelection.SelectedIndex[0]
            : -1; // Or use a suitable fallback value if no selection exists

        if (treeDataGrid.Rows == null || selectedIndexPath == -1)
        {
            return;
        }

        // Convert the logical index to the actual row index in the UI.
        var rowIndex = treeDataGrid.Rows.ModelIndexToRowIndex(selectedIndexPath);

        // Adjust the index if the selected item is a child of a parent row.
        if (treeDataGrid.RowSelection?.SelectedIndex.Count > 1)
        {
            // Skip the first index (parent), sum the child indices, and adjust.
            rowIndex += treeDataGrid.RowSelection.SelectedIndex.Skip(1).Sum();

            // Add 1 to correct the index for proper positioning.
            rowIndex += 1;
        }

        ScrollToItemIndex(rowIndex);
    }

    /// <summary>
    /// Scrolls the <see cref="TreeDataGrid"/> to bring the specified row index into view.
    /// </summary>
    /// <param name="index">The index of the row to bring into view.</param>
    private void ScrollToItemIndex(int index)
    {
        if (AssociatedObject is { RowsPresenter: not null } treeDataGrid)
        {
            treeDataGrid.RowsPresenter.BringIntoView(index);
        }
    }
}
