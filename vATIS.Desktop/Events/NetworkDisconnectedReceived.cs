// <copyright file="NetworkDisconnectedReceived.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Represents an event that is raised when a network disconnect is received.
/// </summary>
public class NetworkDisconnectedReceived : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkDisconnectedReceived"/> class.
    /// </summary>
    /// <param name="callsignInUse">If the disconnect was caused by callsign in use.</param>
    public NetworkDisconnectedReceived(bool callsignInUse = false)
    {
        CallsignInuse = callsignInUse;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the disconnect was because the callsign was in use.
    /// </summary>
    public bool CallsignInuse { get; set; }
}
