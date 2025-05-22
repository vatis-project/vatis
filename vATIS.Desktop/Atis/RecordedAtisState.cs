// <copyright file="RecordedAtisState.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Specifies the possible statuses for a manually recorded ATIS.
/// </summary>
public enum RecordedAtisState
{
    /// <summary>
    /// ATIS is not currently connected or broadcasting.
    /// </summary>
    Disconnected,

    /// <summary>
    /// ATIS is currently connected and actively broadcasting.
    /// </summary>
    Connected,

    /// <summary>
    /// ATIS recording has expired and needs to be updated.
    /// </summary>
    Expired
}
