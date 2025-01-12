// <copyright file="KillRequestReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when a kill request is received.
/// </summary>
public class KillRequestReceived : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KillRequestReceived"/> class.
    /// </summary>
    /// <param name="reason">The kill reason.</param>
    public KillRequestReceived(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets or sets the kill reason.
    /// </summary>
    public string Reason { get; set; }
}
