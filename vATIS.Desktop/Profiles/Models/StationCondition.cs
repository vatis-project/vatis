// <copyright file="StationCondition.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Determines when a static definition is included based on which VATSIM controllers are online.
/// </summary>
/// <remarks>
/// Only used to read profiles saved before conditions were written as text. It is converted to
/// <see cref="StaticDefinition.OnlineCondition"/> when the profile is loaded.
/// </remarks>
public enum StationCondition
{
    /// <summary>
    /// The definition is always included.
    /// </summary>
    Always,

    /// <summary>
    /// The definition is included only while at least one matching callsign is online.
    /// </summary>
    WhenOnline,

    /// <summary>
    /// The definition is included only while no matching callsign is online.
    /// </summary>
    WhenOffline,
}
