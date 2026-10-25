// <copyright file="TreeDataGridKeyboardBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Makes a list of rows with a check box column usable from the keyboard. The whole list is a single Tab stop and the
/// arrow keys move between rows. Tabbing in selects the row, Enter runs the edit command and Space toggles the check
/// box of the row. Alt+Up and Alt+Down run the move commands, for lists that can be reordered.
/// </summary>
public class TreeDataGridKeyboardBehavior : Behavior<TreeDataGrid>
{
    /// <summary>
    /// Defines the <see cref="EditCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> EditCommandProperty =
        AvaloniaProperty.Register<TreeDataGridKeyboardBehavior, ICommand?>(nameof(EditCommand));

    /// <summary>
    /// Defines the <see cref="MoveUpCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> MoveUpCommandProperty =
        AvaloniaProperty.Register<TreeDataGridKeyboardBehavior, ICommand?>(nameof(MoveUpCommand));

    /// <summary>
    /// Defines the <see cref="MoveDownCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> MoveDownCommandProperty =
        AvaloniaProperty.Register<TreeDataGridKeyboardBehavior, ICommand?>(nameof(MoveDownCommand));

    /// <summary>
    /// Defines the <see cref="FocusFirstRow"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> FocusFirstRowProperty =
        AvaloniaProperty.Register<TreeDataGridKeyboardBehavior, bool>(nameof(FocusFirstRow));

    /// <summary>
    /// Gets or sets a value indicating whether the first row is focused when the list is shown, so a dialog that is
    /// only a list can be used from the keyboard straight away.
    /// </summary>
    public bool FocusFirstRow
    {
        get => GetValue(FocusFirstRowProperty);
        set => SetValue(FocusFirstRowProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that Enter runs on the selected row.
    /// </summary>
    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that Alt+Up runs to move the selected row up.
    /// </summary>
    public ICommand? MoveUpCommand
    {
        get => GetValue(MoveUpCommandProperty);
        set => SetValue(MoveUpCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command that Alt+Down runs to move the selected row down.
    /// </summary>
    public ICommand? MoveDownCommand
    {
        get => GetValue(MoveDownCommandProperty);
        set => SetValue(MoveDownCommandProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not { } grid)
        {
            return;
        }

        // Without this, Tab visits every cell and check box of every row before it leaves the list.
        KeyboardNavigation.SetTabNavigation(grid, KeyboardNavigationMode.Once);
        grid.AddHandler(InputElement.GotFocusEvent, OnGotFocus);
        grid.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        grid.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);

        if (FocusFirstRow)
        {
            // The rows do not exist until the list has been laid out.
            grid.LayoutUpdated += OnLayoutUpdatedFocusFirstRow;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } grid)
        {
            grid.LayoutUpdated -= OnLayoutUpdatedFocusFirstRow;
            grid.RemoveHandler(InputElement.GotFocusEvent, OnGotFocus);
            grid.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            grid.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        }

        base.OnDetaching();
    }

    private static TreeDataGridRow? RowOf(object? source)
    {
        return (source as Visual)?.FindAncestorOfType<TreeDataGridRow>(includeSelf: true);
    }

    // The list moves focus between its cells itself, which Avalonia does not count as keyboard focus. Keep track of
    // whether the keyboard was last used, so the focused row can be outlined for it with the :keynav style class.
    private void SetKeyboardNavigation(bool value)
    {
        if (AssociatedObject is { } grid)
        {
            ((IPseudoClasses)grid.Classes).Set(":keynav", value);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SetKeyboardNavigation(false);
    }

    private void OnLayoutUpdatedFocusFirstRow(object? sender, EventArgs e)
    {
        if (AssociatedObject is not { } grid ||
            grid.TryGetRow(0)?.GetVisualDescendants().OfType<TreeDataGridCell>().FirstOrDefault() is not { } cell)
        {
            return;
        }

        grid.LayoutUpdated -= OnLayoutUpdatedFocusFirstRow;
        cell.Focus(NavigationMethod.Tab);
    }

    // Moving a row rebuilds the rows, which drops focus, so it is given back to the row the selection moved to.
    private void FocusSelectedRow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (AssociatedObject is not { RowSelection: { } selection } grid || selection.SelectedIndex.Count == 0)
            {
                return;
            }

            grid.TryGetRow(selection.SelectedIndex[0])?
                .GetVisualDescendants().OfType<TreeDataGridCell>().FirstOrDefault()?
                .Focus(NavigationMethod.Directional);
        });
    }

    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (e.NavigationMethod == NavigationMethod.Tab)
        {
            SetKeyboardNavigation(true);
        }

        // Tabbing into the list selects the row it lands on, so the buttons beside it can act on it straight away.
        if (e.NavigationMethod != NavigationMethod.Tab || AssociatedObject?.RowSelection is not { } selection ||
            selection.SelectedItems.Count > 0 || RowOf(e.Source) is not { RowIndex: >= 0 } row)
        {
            return;
        }

        selection.SelectedIndex = row.RowIndex;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        SetKeyboardNavigation(true);

        if (e.KeyModifiers == KeyModifiers.Alt && e.Key is Key.Up or Key.Down)
        {
            var move = e.Key == Key.Up ? MoveUpCommand : MoveDownCommand;
            if (move != null)
            {
                if (move.CanExecute(null))
                {
                    move.Execute(null);
                    FocusSelectedRow();
                }

                e.Handled = true;
            }

            return;
        }

        if (e.KeyModifiers != KeyModifiers.None)
        {
            return;
        }

        if (e.Key is Key.Enter or Key.Return)
        {
            if (EditCommand is { } edit && edit.CanExecute(null))
            {
                edit.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Space && RowOf(e.Source) is { } row &&
                 row.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault() is { IsEnabled: true } check)
        {
            // The cell checks its model when the check box changes, as it does when the box is clicked.
            check.IsChecked = check.IsChecked != true;
            e.Handled = true;
        }
    }
}
