// <copyright file="Notification.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Notifications;
using Avalonia.Metadata;

namespace Vatsim.Vatis.Ui.Controls.Notification;

/// <summary>
/// A notification that can be shown in a window or by the host operating system.
/// </summary>
/// <remarks>
/// This class represents a notification that can be displayed either in a window using
/// <see cref="WindowNotificationManager"/> or by the host operating system (to be implemented).
/// </remarks>
public class Notification : INotification, INotifyPropertyChanged
{
    private readonly TimeSpan _expiration;
    private string? _title;
    private string? _content;

    /// <summary>
    /// Initializes a new instance of the <see cref="Notification"/> class.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="content">The content to be displayed in the notification.</param>
    /// <param name="type">The <see cref="NotificationType"/> of the notification.</param>
    /// <param name="expiration">The expiry time at which the notification will close.
    /// Use <see cref="TimeSpan.Zero"/> for notifications that will remain open.</param>
    /// <param name="showClose">A value indicating whether the notification should show a close button.</param>
    /// <param name="onClick">An Action to call when the notification is clicked.</param>
    /// <param name="onClose">An Action to call when the notification is closed.</param>
    public Notification(
        string? title,
        string? content,
        NotificationType type = NotificationType.Information,
        TimeSpan? expiration = null,
        bool showClose = true,
        Action? onClick = null,
        Action? onClose = null)
    {
        Title = title;
        Content = content;
        Type = type;
        Expiration = expiration ?? TimeSpan.FromSeconds(3);
        ShowClose = showClose;
        OnClick = onClick;
        OnClose = onClose;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Notification"/> class.
    /// </summary>
    public Notification()
        : this(null, null)
    {
    }

    /// <summary>
    /// Occurs when a property value changes, allowing subscribers to be notified of changes in the object’s properties.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public string? Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    /// <inheritdoc/>
    [Content]
    public string? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }

    /// <inheritdoc/>
    public NotificationType Type { get; set; }

    /// <inheritdoc/>
    public TimeSpan Expiration
    {
        get => _expiration;
        private init
        {
            if (_expiration != value)
            {
                _expiration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowExpirationBar));
            }
        }
    }

    /// <inheritdoc/>
    public bool ShowIcon { get; set; }

    /// <inheritdoc/>
    public bool ShowClose { get; }

    /// <inheritdoc/>
    public Action? OnClick { get; set; }

    /// <inheritdoc/>
    public Action? OnClose { get; set; }

    /// <inheritdoc />
    public bool ShowExpirationBar => Expiration > TimeSpan.Zero;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event to notify listeners that a property value has changed.
    /// </summary>
    /// <param name="propertyName">
    /// The name of the property that has changed. If not provided, it will automatically be set to the
    /// name of the calling property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
