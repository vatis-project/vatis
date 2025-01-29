// <copyright file="Route.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Events.EventBus;

/// <summary>
/// Represents a route between a message type and a handler.
/// </summary>
public class Route
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Route"/> class.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="action">The action.</param>
    public Route(Type messageType, Action<object> action)
    {
        MessageType = messageType;
        HandlerRef = new WeakReference<Action<object>>(action);
    }

    /// <summary>
    /// Gets the message type.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// Gets the target.
    /// </summary>
    public Action<object>? Target
    {
        get { return GetTarget(); }
    }

    private WeakReference<Action<object>> HandlerRef { get; }

    private Action<object>? GetTarget()
    {
        HandlerRef.TryGetTarget(out var target);
        return target;
    }
}
