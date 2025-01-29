// <copyright file="IEventBus.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Events.EventBus;

/// <summary>
/// Interface for the event bus.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe to a message type.
    /// </summary>
    /// <param name="messageType">The type of message to subscribe to.</param>
    /// <param name="handler">The handler to call when the message is published.</param>
    /// <returns>An IDisposable that unsubscribes the handler when disposed.</returns>
    IDisposable Subscribe(Type messageType, Action<object> handler);

    /// <summary>
    /// Publish a message.
    /// </summary>
    /// <param name="message">The message to publish.</param>
    void Publish(object message);
}
