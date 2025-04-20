// <copyright file="NotificationCard.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Notifications;
using Avalonia.LogicalTree;

namespace Vatsim.Vatis.Ui.Controls.Notification;

/// <summary>
/// Control that represents and displays a notification.
/// </summary>
[PseudoClasses(
    WindowNotificationManager.TopLeft,
    WindowNotificationManager.TopRight,
    WindowNotificationManager.BottomLeft,
    WindowNotificationManager.BottomRight,
    WindowNotificationManager.TopCenter,
    WindowNotificationManager.BottomCenter
)]
public class NotificationCard : MessageCard
{
    /// <summary>
    /// Defines the <see cref="Position"/> property for the <see cref="NotificationCard"/> class.
    /// This is a direct Avalonia property, allowing the position to be directly bound to the <see cref="Position"/> property.
    /// </summary>
    public static readonly DirectProperty<NotificationCard, NotificationPosition> PositionProperty =
        AvaloniaProperty.RegisterDirect<NotificationCard, NotificationPosition>(nameof(Position),
            o => o.Position, (o, v) => o.Position = v);

    private NotificationPosition _position;

    /// <summary>
    /// Gets or sets the position of the notification.
    /// </summary>
    public NotificationPosition Position
    {
        get => _position;
        set => SetAndRaise(PositionProperty, ref _position, value);
    }

    /// <inheritdoc />
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdatePseudoClasses(Position);
    }

    private void UpdatePseudoClasses(NotificationPosition position)
    {
        PseudoClasses.Set(WindowNotificationManager.TopLeft, position == NotificationPosition.TopLeft);
        PseudoClasses.Set(WindowNotificationManager.TopRight, position == NotificationPosition.TopRight);
        PseudoClasses.Set(WindowNotificationManager.BottomLeft, position == NotificationPosition.BottomLeft);
        PseudoClasses.Set(WindowNotificationManager.BottomRight, position == NotificationPosition.BottomRight);
        PseudoClasses.Set(WindowNotificationManager.TopCenter, position == NotificationPosition.TopCenter);
        PseudoClasses.Set(WindowNotificationManager.BottomCenter, position == NotificationPosition.BottomCenter);
    }
}
