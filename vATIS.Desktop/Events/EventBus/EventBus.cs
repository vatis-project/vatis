// <copyright file="EventBus.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using Serilog;

namespace Vatsim.Vatis.Events.EventBus;

/// <summary>
/// The event bus.
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentBag<Route> _routes = [];

    /// <summary>
    /// Gets the instance of the event bus.
    /// </summary>
    public static EventBus Instance { get; } = new();

    /// <inheritdoc />
    public void Subscribe(Type messageType, Action<object> handler)
    {
        CleanupRoutes();
        _routes.Add(new Route(messageType, handler));
    }

    /// <inheritdoc />
    public void Publish(object message)
    {
        CleanupRoutes();

        var messageType = message.GetType();
        foreach (var route in _routes)
        {
            if (!route.MessageType.IsAssignableFrom(messageType))
            {
                continue;
            }

            var handler = route.Target;
            if (handler != null)
            {
                InvokeHandler(handler, message);
            }
        }
    }

    private static void InvokeHandler(Action<object>? handler, object message)
    {
        try
        {
            handler?.Invoke(message);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Exception while invoking eventbus handler.");
        }
    }

    private void CleanupRoutes()
    {
        var aliveRoutes = _routes.Where(r => r.Target != null).ToList();
        _routes.Clear();
        foreach (var route in aliveRoutes)
        {
            _routes.Add(route);
        }
    }
}
