// <copyright file="CheckboxToListBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// A behavior for a <see cref="CheckBox"/> that adds or removes a value to/from a bound <see cref="IList"/>
/// based on the checkbox's checked state.
/// </summary>
public class CheckboxToListBehavior : Behavior<CheckBox>
{
    /// <summary>
    /// Identifies the <see cref="ItemsSource"/> Avalonia property.
    /// </summary>
    public static readonly StyledProperty<IList?> ItemsSourceProperty =
        AvaloniaProperty.Register<CheckboxToListBehavior, IList?>(nameof(ItemsSource));

    /// <summary>
    /// Identifies the <see cref="Value"/> Avalonia property.
    /// </summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<CheckboxToListBehavior, object?>(nameof(Value));

    private IDisposable? _itemsSourceSubscription;

    /// <summary>
    /// Gets or sets the list that will be updated based on the checkbox's checked state.
    /// When the checkbox is checked, <see cref="Value"/> is added to this list; when unchecked, it is removed.
    /// </summary>
    public IList? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the value that will be added or removed from the <see cref="ItemsSource"/>.
    /// </summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Called when the behavior is attached to a <see cref="CheckBox"/>.
    /// Subscribes to the <see cref="CheckBox.IsCheckedChanged"/> event.
    /// </summary>
    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject != null)
        {
            AssociatedObject.IsCheckedChanged += OnIsCheckedChanged;
            _itemsSourceSubscription = this.GetObservable(ItemsSourceProperty)
                .Subscribe(_ => UpdateCheckState());
        }
    }

    /// <summary>
    /// Called when the behavior is detached from a <see cref="CheckBox"/>.
    /// Unsubscribes from the event.
    /// </summary>
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject != null)
        {
            AssociatedObject.IsCheckedChanged -= OnIsCheckedChanged;
        }

        _itemsSourceSubscription?.Dispose();
        _itemsSourceSubscription = null;
    }

    private void OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ItemsSource == null || Value == null)
            return;

        if (sender is not CheckBox checkBox)
            return;

        var isChecked = checkBox.IsChecked == true;

        if (isChecked)
        {
            if (!ItemsSource.Contains(Value))
                ItemsSource.Add(Value);
        }
        else
        {
            if (ItemsSource.Contains(Value))
                ItemsSource.Remove(Value);
        }
    }

    /// <summary>
    /// Updates the checkbox's state based on whether the <see cref="Value"/> is contained in the <see cref="ItemsSource"/>.
    /// </summary>
    private void UpdateCheckState()
    {
        if (AssociatedObject != null && ItemsSource != null && Value != null)
        {
            Dispatcher.UIThread.Post(() => AssociatedObject.IsChecked = ItemsSource.Contains(Value));
        }
    }
}
