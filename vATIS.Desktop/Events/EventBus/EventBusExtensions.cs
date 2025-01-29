// <copyright file="EventBusExtensions.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Events.EventBus;

/// <summary>
/// Extension methods for the event bus.
/// </summary>
public static class EventBusExtensions
{
    /// <summary>
    /// Subscribe to a message type.
    /// </summary>
    /// <typeparam name="T">The type of message to subscribe to.</typeparam>
    /// <param name="eventBus">The event bus.</param>
    /// <param name="handler">The handler to call when the message is published.</param>
    /// <returns>An IDisposable that unsubscribes the handler when disposed.</returns>
    public static IDisposable Subscribe<T>(this IEventBus eventBus, Action<T> handler)
    {
        return eventBus.Subscribe(typeof(T), o => handler.Invoke((T)o));
    }
}
