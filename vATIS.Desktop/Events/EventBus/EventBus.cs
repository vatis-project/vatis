// <copyright file="EventBus.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace Vatsim.Vatis.Events.EventBus;

/// <summary>
/// A thread-safe event bus that allows subscribing, unsubscribing, and publishing events.
/// Uses weak references to prevent memory leaks when subscribers are disposed.
/// </summary>
public class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Route>> _routes = new();

    /// <summary>
    /// Gets the singleton instance of the event bus.
    /// </summary>
    public static EventBus Instance { get; } = new();

    /// <summary>
    /// Subscribes a handler to a specific message type and returns an IDisposable for automatic unsubscription.
    /// </summary>
    /// <param name="messageType">The type of message to listen for.</param>
    /// <param name="handler">The event handler to be invoked when the message is published.</param>
    /// <returns>An IDisposable that unsubscribes the handler when disposed.</returns>
    public IDisposable Subscribe(Type messageType, Action<object> handler)
    {
        CleanupRoutes();

        var route = new Route(messageType, handler);

        _routes.AddOrUpdate(
            messageType,
            _ => [route],
            (_, list) =>
            {
                list.Add(route);
                return list;
            }
        );

        return new Unsubscriber(this, messageType, handler);
    }

    /// <summary>
    /// Publishes a message to all subscribed handlers of the corresponding message type.
    /// </summary>
    /// <param name="message">The message instance to publish.</param>
    public void Publish(object message)
    {
        CleanupRoutes();

        var messageType = message.GetType();
        if (_routes.TryGetValue(messageType, out var handlers))
        {
            var handlersCopy = handlers.ToList();
            foreach (var route in handlersCopy)
            {
                Action<object>? handler;
                lock (route)
                {
                    if (!route.TryGetHandler(out handler))
                        continue;
                }

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

    private void Unsubscribe(Type messageType, Action<object> handler)
    {
        if (_routes.TryGetValue(messageType, out var handlers))
        {
            handlers.RemoveAll(r => r.IsMatch(handler));

            if (handlers.Count == 0)
            {
                _routes.TryRemove(messageType, out _);
            }
        }
    }

    private void CleanupRoutes()
    {
        foreach (var key in _routes.Keys)
        {
            if (_routes.TryGetValue(key, out var handlers))
            {
                handlers.RemoveAll(r => !r.IsAlive);

                if (handlers.Count == 0)
                {
                    _routes.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// Disposable object that unsubscribes an event handler when disposed.
    /// </summary>
    private class Unsubscriber : IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly Type _messageType;
        private readonly Action<object> _handler;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Unsubscriber"/> class.
        /// </summary>
        /// <param name="eventBus">The event bus to unsubscribe from.</param>
        /// <param name="messageType">The type of message to unsubscribe from.</param>
        /// <param name="handler">The handler to unsubscribe.</param>
        public Unsubscriber(EventBus eventBus, Type messageType, Action<object> handler)
        {
            _eventBus = eventBus;
            _messageType = messageType;
            _handler = handler;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _eventBus.Unsubscribe(_messageType, _handler);
                _disposed = true;
            }
        }
    }
}
