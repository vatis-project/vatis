// <copyright file="DataGridCellEndEditBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class DataGridCellEndEditBehavior : Behavior<DataGrid>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<DataGridCellEndEditBehavior, ICommand?>(nameof(Command));
    
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null) 
            AssociatedObject.CellEditEnding += AssociatedObjectOnCellEditEnding;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject != null) 
            AssociatedObject.CellEditEnding -= AssociatedObjectOnCellEditEnding;
    }

    private void AssociatedObjectOnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        Command?.Execute(e);
    }
}