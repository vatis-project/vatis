// <copyright file="ConnectionState.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Networking.AtisHub;

/// <summary>
/// Represents the state of a connection to the ATIS hub.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// The connection is not established.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The connection is in the process of being established.
    /// </summary>
    Connecting,

    /// <summary>
    /// The connection is successfully established.
    /// </summary>
    Connected,

    /// <summary>
    /// The connection is in the process of being terminated.
    /// </summary>
    Disconnecting,
}
