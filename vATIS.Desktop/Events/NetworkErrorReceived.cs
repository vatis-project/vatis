// <copyright file="NetworkErrorReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when a network error is received.
/// </summary>
public class NetworkErrorReceived : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkErrorReceived"/> class.
    /// </summary>
    /// <param name="error">The error message.</param>
    public NetworkErrorReceived(string error)
    {
        Error = error;
    }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Error { get; set; }
}
