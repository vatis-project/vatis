// <copyright file="DataGridCellEndEditEventArgEx.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents the event arguments for when a DataGrid cell editing ends.
/// Contains the name of the DataGrid and the original event arguments from the cell edit ending.
/// </summary>
public class DataGridCellEndEditEventArgEx : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataGridCellEndEditEventArgEx"/> class.
    /// </summary>
    /// <param name="dataGridName">The name of the DataGrid associated with this event.</param>
    /// <param name="args">The original <see cref="o:DataGridCellEditEndingEventArgs"/> passed from the DataGrid event.</param>
    public DataGridCellEndEditEventArgEx(string dataGridName, DataGridCellEditEndingEventArgs args)
    {
        DataGridName = dataGridName;
        Args = args;
    }

    /// <summary>
    /// Gets the name of the DataGrid associated with the event.
    /// This can be useful for identifying which DataGrid raised the event when handling multiple DataGrids.
    /// </summary>
    public string DataGridName { get; }

    /// <summary>
    /// Gets the original event arguments from the DataGrid's CellEditEnding event.
    /// This contains information about the cell that was edited, such as the row, column, and the edit action.
    /// </summary>
    public DataGridCellEditEndingEventArgs Args { get; }
}
