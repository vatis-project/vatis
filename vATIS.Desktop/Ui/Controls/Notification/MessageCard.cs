// <copyright file="MessageCard.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Vatsim.Vatis.Ui.Controls.Notification;

/// <summary>
/// Control that represents and displays a message.
/// </summary>
[PseudoClasses(StyleInformation, StyleSuccess, StyleWarning, StyleError)]
public abstract class MessageCard : ContentControl
{
    /// <summary>
    /// Defines the <see cref="IsClosing"/> property.
    /// </summary>
    public static readonly DirectProperty<MessageCard, bool> IsClosingProperty =
        AvaloniaProperty.RegisterDirect<MessageCard, bool>(nameof(IsClosing), o => o.IsClosing);

    /// <summary>
    /// Defines the <see cref="IsClosed"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsClosedProperty =
        AvaloniaProperty.Register<MessageCard, bool>(nameof(IsClosed));

    /// <summary>
    /// Defines the <see cref="NotificationType" /> property.
    /// </summary>
    public static readonly StyledProperty<NotificationType> NotificationTypeProperty =
        AvaloniaProperty.Register<MessageCard, NotificationType>(nameof(NotificationType));

    /// <summary>
    /// Defines the <see cref="ShowIcon"/> property for the <see cref="MessageCard"/> class.
    /// This property determines whether an icon should be shown in the message card.
    /// </summary>
    public static readonly StyledProperty<bool> ShowIconProperty =
        AvaloniaProperty.Register<MessageCard, bool>(nameof(ShowIcon), true);

    /// <summary>
    /// Defines the <see cref="ShowClose"/> property for the <see cref="MessageCard"/> class.
    /// This property determines whether the close button should be shown in the message card.
    /// </summary>
    public static readonly StyledProperty<bool> ShowCloseProperty =
        AvaloniaProperty.Register<MessageCard, bool>(nameof(ShowClose), true);

    /// <summary>
    /// Defines the <see cref="MessageClosed"/> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> MessageClosedEvent =
        RoutedEvent.Register<MessageCard, RoutedEventArgs>(nameof(MessageClosed), RoutingStrategies.Bubble);

    /// <summary>
    /// Defines the CloseOnClick property.
    /// </summary>
    public static readonly AttachedProperty<bool> CloseOnClickProperty =
        AvaloniaProperty.RegisterAttached<MessageCard, Button, bool>("CloseOnClick", defaultValue: false);

    /// <summary>
    /// Defines the <see cref="Progress"/> property. This represents the percentage
    /// (from 0 to 100) of time remaining before the notification automatically closes.
    /// </summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<NotificationCard, double>(nameof(Progress), 100);

    /// <summary>
    /// Defines a property that shows or hides the expiration bar.
    /// </summary>
    public static readonly StyledProperty<bool> ShowExpirationBarProperty =
        AvaloniaProperty.Register<NotificationCard, bool>(nameof(ShowExpirationBar));

    private const string StyleInformation = ":information";
    private const string StyleSuccess = ":success";
    private const string StyleWarning = ":warning";
    private const string StyleError = ":error";

    private bool _isClosing;

    static MessageCard()
    {
        CloseOnClickProperty.Changed.AddClassHandler<Button>(OnCloseOnClickPropertyChanged);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageCard"/> class.
    /// </summary>
    protected MessageCard()
    {
        UpdateNotificationType();
    }

    /// <summary>
    /// Raised when the <see cref="MessageCard"/> has closed.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? MessageClosed
    {
        add => AddHandler(MessageClosedEvent, value);
        remove => RemoveHandler(MessageClosedEvent, value);
    }

    /// <summary>
    /// Gets a value indicating whether the message is already closing.
    /// </summary>
    public bool IsClosing
    {
        get => _isClosing;
        private set => SetAndRaise(IsClosingProperty, ref _isClosing, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the message is closed.
    /// </summary>
    public bool IsClosed
    {
        get => GetValue(IsClosedProperty);
        set => SetValue(IsClosedProperty, value);
    }

    /// <summary>
    /// Gets the type of the message.
    /// </summary>
    public NotificationType NotificationType
    {
        get => GetValue(NotificationTypeProperty);
        init => SetValue(NotificationTypeProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the message should show an icon.
    /// </summary>
    public bool ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the message should show a close button.
    /// </summary>
    public bool ShowClose
    {
        get => GetValue(ShowCloseProperty);
        set => SetValue(ShowCloseProperty, value);
    }

    /// <summary>
    /// Gets or sets the progress value (0 to 100) that represents the remaining time
    /// before the notification closes. A value of 100 means full duration remaining;
    /// 0 means it's about to close.
    /// </summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the expiration bar should be shown.
    /// </summary>
    public bool ShowExpirationBar
    {
        get => GetValue(ShowExpirationBarProperty);
        set => SetValue(ShowExpirationBarProperty, value);
    }

    /// <summary>
    /// Gets the value of the <see cref="CloseOnClickProperty"/> for the specified <see cref="Button"/>.
    /// </summary>
    /// <param name="obj">The <see cref="Button"/> instance from which to retrieve the value.</param>
    /// <returns>The value of the <see cref="CloseOnClickProperty"/> for the specified button.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="obj"/> is <c>null</c>.</exception>
    public static bool GetCloseOnClick(Button obj)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        return obj.GetValue(CloseOnClickProperty);
    }

    /// <summary>
    /// Sets the value of the <see cref="CloseOnClickProperty"/> for the specified <see cref="Button"/>.
    /// </summary>
    /// <param name="obj">The <see cref="Button"/> instance on which to set the value.</param>
    /// <param name="value">The value to set for the <see cref="CloseOnClickProperty"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="obj"/> is <c>null</c>.</exception>
    public static void SetCloseOnClick(Button obj, bool value)
    {
        _ = obj ?? throw new ArgumentNullException(nameof(obj));
        obj.SetValue(CloseOnClickProperty, value);
    }

    /// <summary>
    /// Closes the <see cref="MessageCard"/>.
    /// </summary>
    public void Close()
    {
        if (IsClosing)
        {
            return;
        }

        IsClosing = true;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ContentProperty && e.NewValue is IMessage message)
        {
            SetValue(NotificationTypeProperty, message.Type);
        }

        if (e.Property == NotificationTypeProperty)
        {
            UpdateNotificationType();
        }

        if (e.Property == IsClosedProperty)
        {
            if (!IsClosing && !IsClosed)
            {
                return;
            }

            RaiseEvent(new RoutedEventArgs(MessageClosedEvent));
        }
    }

    private static void OnCloseOnClickPropertyChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
    {
        var button = (Button)d;
        var value = (bool)e.NewValue!;
        if (value)
        {
            button.Click += Button_Click;
        }
        else
        {
            button.Click -= Button_Click;
        }
    }

    /// <summary>
    /// Called when a button inside the Message is clicked.
    /// </summary>
    private static void Button_Click(object? sender, RoutedEventArgs e)
    {
        var btn = sender as ILogical;
        var message = btn?.GetLogicalAncestors().OfType<MessageCard>().FirstOrDefault();
        message?.Close();
    }

    private void UpdateNotificationType()
    {
        switch (NotificationType)
        {
            case NotificationType.Error:
                PseudoClasses.Add(StyleError);
                break;

            case NotificationType.Information:
                PseudoClasses.Add(StyleInformation);
                break;

            case NotificationType.Success:
                PseudoClasses.Add(StyleSuccess);
                break;

            case NotificationType.Warning:
                PseudoClasses.Add(StyleWarning);
                break;
        }
    }
}
