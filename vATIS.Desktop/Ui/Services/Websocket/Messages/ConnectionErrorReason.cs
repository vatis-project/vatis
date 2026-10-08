// <copyright file="ConnectionErrorReason.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Ui.Services.Websocket.Messages;

/// <summary>
/// Why a station failed to connect to the network, or lost its connection.
/// </summary>
public enum ConnectionErrorReason
{
    /// <summary>
    /// The maximum number of ATIS connections is already in use.
    /// </summary>
    TooManyConnections,

    /// <summary>
    /// The VATSIM user ID, password, or real name has not been set.
    /// </summary>
    ConfigurationRequired,

    /// <summary>
    /// The connection to the network could not be made.
    /// </summary>
    ConnectionFailed,

    /// <summary>
    /// The network reported an error, such as the callsign being in use or invalid credentials.
    /// </summary>
    NetworkError,

    /// <summary>
    /// The network disconnected the station.
    /// </summary>
    Killed,
}
