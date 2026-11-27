// <copyright file="SelectableList.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace Vatsim.Vatis.Ui.Models;

/// <summary>
/// The rows shown in a list and which of them are selected, for a view model to bind a grid to.
/// </summary>
/// <typeparam name="T">The row type.</typeparam>
public sealed class SelectableList<T> : ReactiveObject
    where T : class
{
    private IReadOnlyList<T> _items = [];
    private int _selectedIndex = -1;

    /// <summary>
    /// Gets or sets the rows, in display order.
    /// </summary>
    public IReadOnlyList<T> Items
    {
        get => _items;
        set
        {
            this.RaiseAndSetIfChanged(ref _items, value);
            this.RaisePropertyChanged(nameof(SelectedItem));
        }
    }

    /// <summary>
    /// Gets or sets the index of the selected row, or -1 when nothing is selected. With several rows selected, this
    /// is the first one.
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedIndex, value);
            this.RaisePropertyChanged(nameof(SelectedItem));
        }
    }

    /// <summary>
    /// Gets the selected row, or <see langword="null"/> when nothing is selected.
    /// </summary>
    public T? SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;

    /// <summary>
    /// Gets every selected row. The grid keeps it up to date.
    /// </summary>
    public ObservableCollection<T> SelectedItems { get; } = [];

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectedIndex = -1;
        SelectedItems.Clear();
    }
}
