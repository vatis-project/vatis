// <copyright file="ScrollToSelectedItemBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Xaml.Interactions.Custom;
using ReactiveUI;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Behavior that automatically scrolls a <see cref="TreeDataGrid"/> to ensure the selected item is visible.
/// </summary>
public class ScrollToSelectedItemBehavior : AttachedToVisualTreeBehavior<TreeDataGrid>
{
    /// <inheritdoc />
    /// <summary>
    /// Attaches to the visual tree and subscribes to the selection change event
    /// to scroll the selected item into view.
    /// </summary>
    /// <param name="disposable">A composite disposable to manage subscriptions.</param>
    protected override void OnAttachedToVisualTree(CompositeDisposable disposable)
    {
        if (AssociatedObject is { RowSelection: { } rowSelection })
        {
            Observable.FromEventPattern(rowSelection, nameof(rowSelection.SelectionChanged))
                .Select(_ =>
                {
                    // Retrieve the first selected index.
                    var selectedIndexPath = rowSelection.SelectedIndex.FirstOrDefault();
                    if (AssociatedObject.Rows is null)
                    {
                        return selectedIndexPath;
                    }

                    // Convert the logical index to the actual row index in the UI.
                    var rowIndex = AssociatedObject.Rows.ModelIndexToRowIndex(selectedIndexPath);

                    // Adjust the index if the selected item is a child of a parent row.
                    if (rowSelection.SelectedIndex.Count > 1)
                    {
                        // Skip the first index (parent), sum the child indices, and adjust.
                        rowIndex += rowSelection.SelectedIndex.Skip(1).Sum();

                        // Add 1 to correct the index for proper positioning.
                        rowIndex += 1;
                    }

                    return rowIndex;
                })
                .WhereNotNull()
                .Do(ScrollToItemIndex)
                .Subscribe()
                .DisposeWith(disposable);
        }
    }

    /// <summary>
    /// Scrolls the <see cref="TreeDataGrid"/> to bring the specified row index into view.
    /// </summary>
    /// <param name="index">The index of the row to bring into view.</param>
    private void ScrollToItemIndex(int index)
    {
        if (AssociatedObject is { RowsPresenter: { } rowsPresenter })
        {
            rowsPresenter.BringIntoView(index);
        }
    }
}
