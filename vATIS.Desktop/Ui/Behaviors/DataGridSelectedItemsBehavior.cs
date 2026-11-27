// <copyright file="DataGridSelectedItemsBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Copies the selected rows of a <see cref="DataGrid"/> into a list whenever the selection changes, since the grid's
/// own <see cref="DataGrid.SelectedItems"/> cannot be bound.
/// </summary>
public class DataGridSelectedItemsBehavior : Behavior<DataGrid>
{
    /// <summary>
    /// Defines the <see cref="SelectedItems"/> property.
    /// </summary>
    public static readonly StyledProperty<IList?> SelectedItemsProperty =
        AvaloniaProperty.Register<DataGridSelectedItemsBehavior, IList?>(nameof(SelectedItems));

    /// <summary>
    /// Gets or sets the list that receives the selected rows.
    /// </summary>
    public IList? SelectedItems
    {
        get => GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.SelectionChanged += OnSelectionChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.SelectionChanged -= OnSelectionChanged;
        }

        base.OnDetaching();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SelectedItems is not { } target || AssociatedObject is not { } grid)
        {
            return;
        }

        target.Clear();
        foreach (var item in grid.SelectedItems)
        {
            target.Add(item);
        }
    }
}
