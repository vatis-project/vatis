// <copyright file="IMessage.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia.Controls.Notifications;

namespace Vatsim.Vatis.Ui.Controls.Notification;

/// <summary>
/// Represents a message that can be shown in a window or by the host operating system.
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Gets the <see cref="NotificationType"/> of the message.
    /// </summary>
    public NotificationType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the message should show an icon.
    /// </summary>
    public bool ShowIcon { get; }

    /// <summary>
    /// Gets a value indicating whether the message should show a close button.
    /// </summary>
    public bool ShowClose { get; }

    /// <summary>
    /// Gets the expiration time of the message, after which it will automatically close.
    /// If the value is <see cref="TimeSpan.Zero"/> then the message will remain open until the user closes it.
    /// </summary>
    public TimeSpan Expiration { get; }

    /// <summary>
    /// Gets an Action to be run when the message is clicked.
    /// </summary>
    public Action? OnClick { get; }

    /// <summary>
    /// Gets an Action to be run when the message is closed.
    /// </summary>
    public Action? OnClose { get; }

    /// <summary>
    /// Gets a value indicating whether the message should show an expiration countdown bar.
    /// </summary>
    public bool ShowExpirationBar { get; }
}
