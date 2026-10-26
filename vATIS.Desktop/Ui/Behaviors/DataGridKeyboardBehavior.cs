// <copyright file="DataGridKeyboardBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Makes a list that has Edit and Delete buttons usable from the keyboard. Tabbing into the list selects its first row
/// when nothing is selected, so the buttons can act on it straight away, and Enter runs the edit command on the
/// selected row.
/// </summary>
public class DataGridKeyboardBehavior : Behavior<DataGrid>
{
    /// <summary>
    /// Defines the <see cref="EditCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> EditCommandProperty =
        AvaloniaProperty.Register<DataGridKeyboardBehavior, ICommand?>(nameof(EditCommand));

    /// <summary>
    /// Gets or sets the command that Enter runs. The selected row is passed to it.
    /// </summary>
    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject?.AddHandler(InputElement.GotFocusEvent, OnGotFocus);
        AssociatedObject?.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        AssociatedObject?.RemoveHandler(InputElement.GotFocusEvent, OnGotFocus);
        AssociatedObject?.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        base.OnDetaching();
    }

    private void OnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (e.NavigationMethod == NavigationMethod.Tab && AssociatedObject is { SelectedItem: null } grid &&
            grid.ItemsSource != null && grid.SelectedIndex < 0)
        {
            grid.SelectedIndex = 0;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return) || e.KeyModifiers != KeyModifiers.None ||
            AssociatedObject?.SelectedItem is not { } item || EditCommand is not { } edit ||
            !edit.CanExecute(item))
        {
            return;
        }

        // A text cell being edited still needs Enter to commit it.
        if (e.Source is TextBox)
        {
            return;
        }

        edit.Execute(item);
        e.Handled = true;
    }
}
