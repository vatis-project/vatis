// <copyright file="AtisType.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace DevServer.Models;

/// <summary>
/// Represents the type of ATIS.
/// </summary>
public enum AtisType
{
    /// <summary>
    /// Represents a combined ATIS.
    /// </summary>
    Combined,

    /// <summary>
    /// Represents a departure ATIS.
    /// </summary>
    Departure,

    /// <summary>
    /// Represents an arrival ATIS.
    /// </summary>
    Arrival
}
