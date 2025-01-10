// <copyright file="DataGridCellEndEditBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

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
    /// Gets or sets the command to be executed when cell editing ends in a DataGrid control.
    /// </summary>
    public ICommand? Command
    {
        get => this.GetValue(CommandProperty);
        set => this.SetValue(CommandProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.CellEditEnding += this.AssociatedObjectOnCellEditEnding;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.CellEditEnding -= this.AssociatedObjectOnCellEditEnding;
        }
    }

    private void AssociatedObjectOnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        this.Command?.Execute(e);
    }
}
