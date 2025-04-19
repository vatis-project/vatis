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
    protected override IDisposable OnAttachedToVisualTreeOverride()
    {
        if (AssociatedObject is not { RowSelection: { } rowSelection })
        {
            return Disposable.Empty;
        }

        return Observable.FromEventPattern(rowSelection, nameof(rowSelection.SelectionChanged))
            .Select(_ =>
            {
                var selectedIndexPath = rowSelection.SelectedIndex.FirstOrDefault();
                if (AssociatedObject.Rows is null)
                {
                    return selectedIndexPath;
                }

                // Get the actual index in the list of items.
                var rowIndex = AssociatedObject.Rows.ModelIndexToRowIndex(selectedIndexPath);

                // Correct the index wih the index of child item, in the case when the selected item is a child.
                if (rowSelection.SelectedIndex.Count > 1)
                {
                    // Skip 1 because the first index is the parent.
                    // Every other index is the child index.
                    rowIndex += rowSelection.SelectedIndex.Skip(1).Sum();

                    // Need to add 1 to get the correct index.
                    rowIndex += 1;
                }

                return rowIndex;
            })
            .WhereNotNull()
            .Do(ScrollToItemIndex)
            .Subscribe();
    }

    private void ScrollToItemIndex(int index)
    {
        if (AssociatedObject is { RowsPresenter: { } rowsPresenter })
        {
            rowsPresenter.BringIntoView(index);
        }
    }
}
