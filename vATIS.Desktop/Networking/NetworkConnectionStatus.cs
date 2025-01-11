// <copyright file="NetworkConnectionStatus.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Networking;

/// <summary>
/// Specifies the possible statuses for a network connection.
/// </summary>
public enum NetworkConnectionStatus
{
    /// <summary>
    /// Represents the state where the network connection is established and active.
    /// </summary>
    Connected,

    /// <summary>
    /// Represents the state where a network connection is in the process of being established.
    /// </summary>
    Connecting,

    /// <summary>
    /// Represents the state where the network connection is not established or active.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Represents the observer state, used when subscribed to an ATIS hosted by another user.
    /// </summary>
    Observer,
}
