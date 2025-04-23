// <copyright file="INotification.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Controls.Notification;

/// <summary>
/// Represents a notification that can be shown in a window or by the host operating system.
/// </summary>
public interface INotification : IMessage
{
    /// <summary>
    /// Gets the Title of the notification.
    /// </summary>
    public string? Title { get; }

    /// <summary>
    /// Gets the Content of the notification.
    /// </summary>
    public string? Content { get; }
}
