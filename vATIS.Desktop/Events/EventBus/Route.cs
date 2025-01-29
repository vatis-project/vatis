// <copyright file="Route.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Events.EventBus;

/// <summary>
/// Represents a route for event handling, storing a weak reference to the event handler.
/// This prevents memory leaks by allowing handlers to be garbage-collected when they go out of scope.
/// </summary>
public class Route
{
    private readonly WeakReference<Action<object>> _weakHandler;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Route"/> class.
    /// </summary>
    /// <param name="messageType">The type of message to listen for.</param>
    /// <param name="handler">The event handler to be invoked when the message is published.</param>
    public Route(Type messageType, Action<object> handler)
    {
        MessageType = messageType;
        _weakHandler = new WeakReference<Action<object>>(handler);
    }

    /// <summary>
    /// Gets the type of message this route handles.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// Gets a value indicating whether the handler is still alive (not garbage collected).
    /// </summary>
    public bool IsAlive => _weakHandler.TryGetTarget(out _);

    /// <summary>
    /// Attempts to retrieve the event handler if it is still alive.
    /// </summary>
    /// <param name="handler">The retrieved handler, or null if it has been garbage collected.</param>
    /// <returns>True if the handler is still alive, false if it has been collected.</returns>
    public bool TryGetHandler(out Action<object>? handler)
    {
        lock (_lock)
        {
            return _weakHandler.TryGetTarget(out handler);
        }
    }

    /// <summary>
    /// Checks if the provided handler matches the stored handler.
    /// </summary>
    /// <param name="handler">The handler to check against.</param>
    /// <returns>True if the stored handler matches the provided handler, otherwise false.</returns>
    public bool IsMatch(Action<object> handler)
    {
        return _weakHandler.TryGetTarget(out var target) && target == handler;
    }
}
