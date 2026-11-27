// <copyright file="DataGridKeyboardBehavior.cs" company="Justin Shannon">
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
/// Makes a list that has Edit and Delete buttons usable from the keyboard. Tabbing into the list selects its first row
/// when nothing is selected, so the buttons can act on it straight away, and Enter runs the edit command on the
/// selected row. Space toggles the check box of the selected row, and Alt+Up and Alt+Down run the move commands, for
/// lists that can be reordered. While the list is used from the keyboard it has the <c>:keynav</c> pseudo-class, so the
/// focus box can be shown only then.
/// </summary>
public class DataGridKeyboardBehavior : Behavior<DataGrid>
{
    /// <summary>
    /// Defines the <see cref="EditCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> EditCommandProperty =
        AvaloniaProperty.Register<DataGridKeyboardBehavior, ICommand?>(nameof(EditCommand));

    /// <summary>
    /// Defines the <see cref="PassSelectedItem"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> PassSelectedItemProperty =
        AvaloniaProperty.Register<DataGridKeyboardBehavior, bool>(nameof(PassSelectedItem), defaultValue: true);

    /// <summary>
    /// Defines the <see cref="MoveUpCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> MoveUpCommandProperty =
        AvaloniaProperty.Register<DataGridKeyboardBehavior, ICommand?>(nameof(MoveUpCommand));

    /// <summary>
    /// Defines the <see cref="MoveDownCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> MoveDownCommandProperty =
        AvaloniaProperty.Register<DataGridKeyboardBehavior, ICommand?>(nameof(MoveDownCommand));

    /// <summary>
    /// Defines the <see cref="FocusFirstRow"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> FocusFirstRowProperty =
        AvaloniaProperty.Register<DataGridKeyboardBehavior, bool>(nameof(FocusFirstRow));

    /// <summary>
    /// Gets or sets the command that Enter runs.
    /// </summary>
    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the selected row is passed to <see cref="EditCommand"/>. Turn it off for
    /// a command that takes no parameter and reads the selection itself.
    /// </summary>
    public bool PassSelectedItem
    {
        get => GetValue(PassSelectedItemProperty);
        set => SetValue(PassSelectedItemProperty, value);
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

    /// <summary>
    /// Gets or sets a value indicating whether the list takes focus and selects its first row when it is shown, so a
    /// dialog that is only a list can be used from the keyboard straight away.
    /// </summary>
    public bool FocusFirstRow
    {
        get => GetValue(FocusFirstRowProperty);
        set => SetValue(FocusFirstRowProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not { } grid)
        {
            return;
        }

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

    private void OnLayoutUpdatedFocusFirstRow(object? sender, EventArgs e)
    {
        if (AssociatedObject is not { } grid || !grid.GetVisualDescendants().OfType<DataGridRow>().Any())
        {
            return;
        }

        grid.LayoutUpdated -= OnLayoutUpdatedFocusFirstRow;

        // Focus without the focus box, which only shows once the keyboard is used.
        grid.Focus();
        if (grid.SelectedIndex < 0)
        {
            grid.SelectedIndex = 0;
        }
    }

    // DataGrid shows its focus box whenever it has focus. Keep track of whether the keyboard was last used, so styles
    // can show the box only for keyboard use.
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

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.NavigationMethod == NavigationMethod.Tab)
        {
            SetKeyboardNavigation(true);
        }

        if (e.NavigationMethod == NavigationMethod.Tab && AssociatedObject is { SelectedItem: null } grid &&
            grid.ItemsSource != null && grid.SelectedIndex < 0)
        {
            grid.SelectedIndex = 0;
        }
    }

    // Moving a row replaces the rows, so focus is given back to the list and the moved row is scrolled into view.
    private void FocusSelectedRow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (AssociatedObject is not { } grid)
            {
                return;
            }

            grid.Focus(NavigationMethod.Directional);
            if (grid.SelectedItem is { } item)
            {
                grid.ScrollIntoView(item, null);
            }
        });
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        SetKeyboardNavigation(true);

        // A text cell being edited still needs its keys.
        if (e.Source is TextBox || AssociatedObject is not { } grid)
        {
            return;
        }

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

        if (e.KeyModifiers != KeyModifiers.None || grid.SelectedItem is not { } item)
        {
            return;
        }

        if (e.Key is Key.Enter or Key.Return)
        {
            var parameter = PassSelectedItem ? item : null;
            if (EditCommand is { } edit && edit.CanExecute(parameter))
            {
                edit.Execute(parameter);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Space &&
                 grid.GetVisualDescendants().OfType<DataGridRow>().FirstOrDefault(r => r.DataContext == item) is { } row &&
                 row.GetVisualDescendants().OfType<CheckBox>().FirstOrDefault() is { IsEnabled: true } check)
        {
            check.IsChecked = check.IsChecked != true;
            e.Handled = true;
        }
    }
}
