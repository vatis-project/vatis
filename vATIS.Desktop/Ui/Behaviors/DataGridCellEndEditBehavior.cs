// <copyright file="DataGridCellEndEditBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Provides behavior to execute a command when cell editing ends in a DataGrid control.
/// </summary>
public class DataGridCellEndEditBehavior : Behavior<DataGrid>
{
    /// <summary>
    /// Identifies the <see cref="Command"/> dependency property, which stores the command to execute
    /// when cell editing ends in a DataGrid control.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<DataGridCellEndEditBehavior, ICommand?>(nameof(Command));

    /// <summary>
    /// Identifies the name of the DataGrid from which this command is executed from.
    /// </summary>
    public static readonly StyledProperty<string> DataGridNameProperty =
        AvaloniaProperty.Register<DataGridCellEndEditBehavior, string>(nameof(DataGridName));

    /// <summary>
    /// Gets or sets the command to be executed when cell editing ends in a DataGrid control.
    /// </summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the name of the DataGrid.
    /// </summary>
    public string DataGridName
    {
        get => GetValue(DataGridNameProperty);
        set => SetValue(DataGridNameProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.CellEditEnding += AssociatedObjectOnCellEditEnding;
            AssociatedObject.AddHandler(SelectingItemsControl.SelectionChangedEvent, OnEditorSelectionChanged);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject != null)
        {
            AssociatedObject.CellEditEnding -= AssociatedObjectOnCellEditEnding;
            AssociatedObject.RemoveHandler(SelectingItemsControl.SelectionChangedEvent, OnEditorSelectionChanged);
        }
    }

    private void OnEditorSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // A combo box editor only commits when it loses focus, so commit as soon as the user picks a new value.
        // The initial binding fires with nothing removed, which is ignored.
        if (e.Source is ComboBox && e.RemovedItems.Count > 0 && e.AddedItems.Count > 0)
        {
            Dispatcher.UIThread.Post(() => AssociatedObject?.CommitEdit());
        }
    }

    private void AssociatedObjectOnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        var args = new DataGridCellEndEditEventArgEx(DataGridName, e);
        Command?.Execute(args);
    }
}
