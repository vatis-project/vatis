// <copyright file="WindowNotificationManager.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Vatsim.Vatis.Ui.Controls.Notification;

/// <summary>
/// Manages and displays notifications.
/// </summary>
[PseudoClasses(TopLeft, TopRight, BottomLeft, BottomRight, TopCenter, BottomCenter)]
public class WindowNotificationManager : WindowMessageManager
{
    /// <summary>
    /// Top Left Position.
    /// </summary>
    public const string TopLeft = ":topleft";

    /// <summary>
    /// Top Right Position.
    /// </summary>
    public const string TopRight = ":topright";

    /// <summary>
    /// Bottom Left Position.
    /// </summary>
    public const string BottomLeft = ":bottomleft";

    /// <summary>
    /// Bottom Right Position.
    /// </summary>
    public const string BottomRight = ":bottomright";

    /// <summary>
    /// Top Center Position.
    /// </summary>
    public const string TopCenter = ":topcenter";

    /// <summary>
    /// Bottom Center Position.
    /// </summary>
    public const string BottomCenter = ":bottomcenter";

    /// <summary>
    /// Defines a property to set the position of the notification cards.
    /// </summary>
    public static readonly StyledProperty<NotificationPosition> PositionProperty =
        AvaloniaProperty.Register<WindowNotificationManager, NotificationPosition>(nameof(Position),
            NotificationPosition.TopRight);

    private int _hoveredCards;
    private bool _isWindowActivated;

    /// <summary>
    /// Initializes static members of the <see cref="WindowNotificationManager"/> class.
    /// Sets default values for <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/> to <see cref="Stretch"/>.
    /// </summary>
    static WindowNotificationManager()
    {
        HorizontalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(HorizontalAlignment.Stretch);
        VerticalAlignmentProperty.OverrideDefaultValue<WindowNotificationManager>(VerticalAlignment.Stretch);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNotificationManager"/> class.
    /// </summary>
    /// <param name="host">The TopLevel that will host the control.</param>
    public WindowNotificationManager(TopLevel? host)
        : this()
    {
        if (host is not null)
        {
            InstallFromTopLevel(host);
            if (host is Window window)
            {
                window.Activated += (_, _) => _isWindowActivated = true;
                window.Deactivated += (_, _) => _isWindowActivated = false;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNotificationManager"/> class,
    /// optionally associating it with a <see cref="VisualLayerManager"/>.
    /// </summary>
    /// <param name="visualLayerManager">
    /// The <see cref="VisualLayerManager"/> used to manage visual layers, or <c>null</c> to use default behavior.
    /// </param>
    public WindowNotificationManager(VisualLayerManager? visualLayerManager)
        : base(visualLayerManager)
    {
        UpdatePseudoClasses(Position);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowNotificationManager"/> class.
    /// </summary>
    private WindowNotificationManager()
    {
        UpdatePseudoClasses(Position);
    }

    /// <summary>
    /// Gets or sets the corner of the screen notifications can be displayed in.
    /// </summary>
    /// <seealso cref="NotificationPosition"/>
    public NotificationPosition Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    private bool ShouldPause => _hoveredCards > 0 || !_isWindowActivated;

    /// <summary>
    /// Tries to get the <see cref="WindowNotificationManager"/> from a <see cref="Visual"/>.
    /// </summary>
    /// <param name="visual">A <see cref="Visual"/> that is either a <see cref="Window"/> or a <see cref="VisualLayerManager"/>.</param>
    /// <param name="manager">The existing <see cref="WindowNotificationManager"/> if found, or null if not found.</param>
    /// <returns>True if a <see cref="WindowNotificationManager"/> is found; otherwise, false.</returns>
    public static bool TryGetNotificationManager(Visual? visual, out WindowNotificationManager? manager)
    {
        manager = visual?.FindDescendantOfType<WindowNotificationManager>();
        return manager is not null;
    }

    /// <summary>
    /// Shows a notification.
    /// </summary>
    /// <param name="content">The content of the notification.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="expiration">
    ///     The expiration time of the notification, after which it will automatically close.
    ///     If the value is <see cref="TimeSpan.Zero"/>, the notification will remain open until the user closes it.
    /// </param>
    /// <param name="showIcon">Whether to show the icon.</param>
    /// <param name="showClose">Whether to show the close button.</param>
    /// <param name="onClick">An action to be run when the notification is clicked.</param>
    /// <param name="onClose">An action to be run when the notification is closed.</param>
    /// <param name="classes">Style classes to apply.</param>
    public void Show(object content,
        NotificationType type,
        TimeSpan? expiration = null,
        bool showIcon = true,
        bool showClose = true,
        Action? onClick = null,
        Action? onClose = null,
        string[]? classes = null)
    {
        Dispatcher.UIThread.VerifyAccess();

        var notificationControl = new NotificationCard
        {
            Content = content,
            NotificationType = type,
            ShowIcon = showIcon,
            ShowClose = showClose,
            ShowExpirationBar = expiration != TimeSpan.Zero,
            [!NotificationCard.PositionProperty] = this[!PositionProperty]
        };

        notificationControl.Classes.Add("Light");

        // Add style classes if any
        if (classes is not null)
        {
            foreach (var @class in classes)
            {
                notificationControl.Classes.Add(@class);
            }
        }

        notificationControl.MessageClosed += (sender, _) =>
        {
            onClose?.Invoke();
            Items?.Remove(sender);
        };

        notificationControl.PointerPressed += (_, _) => onClick?.Invoke();
        notificationControl.PointerEntered += (_, _) => _hoveredCards++;
        notificationControl.PointerExited += (_, _) =>
        {
            if (_hoveredCards > 0)
                _hoveredCards--;
        };

        Dispatcher.UIThread.Post(() =>
        {
            Items?.Add(notificationControl);

            if (Items?.OfType<NotificationCard>().Count(i => !i.IsClosing) > MaxItems)
            {
                Items.OfType<NotificationCard>().First(i => !i.IsClosing).Close();
            }
        });

        if (expiration != TimeSpan.Zero)
        {
            var updateInterval = TimeSpan.FromMilliseconds(16); // ~60fps
            var totalMs = (expiration ?? TimeSpan.FromSeconds(15)).TotalMilliseconds;
            var elapsed = 0.0;

            var timer = new DispatcherTimer(updateInterval, DispatcherPriority.Normal, (s, _) =>
            {
                if (ShouldPause)
                {
                    return;
                }

                elapsed += updateInterval.TotalMilliseconds;
                var progress = 100 - (elapsed / totalMs * 100);
                notificationControl.Progress = progress;

                if (elapsed >= totalMs)
                {
                    ((DispatcherTimer)s!).Stop();
                    notificationControl.Close();
                }
            });

            timer.Start();
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PositionProperty)
        {
            UpdatePseudoClasses(change.GetNewValue<NotificationPosition>());
        }
    }

    private void UpdatePseudoClasses(NotificationPosition position)
    {
        PseudoClasses.Set(TopLeft, position == NotificationPosition.TopLeft);
        PseudoClasses.Set(TopRight, position == NotificationPosition.TopRight);
        PseudoClasses.Set(BottomLeft, position == NotificationPosition.BottomLeft);
        PseudoClasses.Set(BottomRight, position == NotificationPosition.BottomRight);
        PseudoClasses.Set(TopCenter, position == NotificationPosition.TopCenter);
        PseudoClasses.Set(BottomCenter, position == NotificationPosition.BottomCenter);
    }
}
