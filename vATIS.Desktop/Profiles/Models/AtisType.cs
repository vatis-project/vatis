// <copyright file="AtisType.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents the type of ATIS.
/// </summary>
public enum AtisType
{
    /// <summary>
    /// Represents a combined ATIS type that includes both arrival and departure information.
    /// </summary>
    Combined,

    /// <summary>
    /// Represents an ATIS type for departure information.
    /// </summary>
    Departure,

    /// <summary>
    /// Represents an ATIS type for arrival information.
    /// </summary>
    Arrival,
}
