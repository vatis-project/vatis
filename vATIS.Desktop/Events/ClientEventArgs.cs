// <copyright file="ClientEventArgs.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when a client event occurs.
/// </summary>
/// <typeparam name="T">The type of the event.</typeparam>
public class ClientEventArgs<T> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientEventArgs{T}"/> class.
    /// </summary>
    /// <param name="value">The event value.</param>
    public ClientEventArgs(T value)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets or sets the value associated with the event.
    /// </summary>
    public T Value { get; set; }
}
