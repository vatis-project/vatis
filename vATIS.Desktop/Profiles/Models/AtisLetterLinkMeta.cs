// <copyright file="AtisLetterLinkMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Describes how a departure station's ATIS letter is tied to the arrival station's letter at the same airport.
/// </summary>
public class AtisLetterLinkMeta
{
    /// <summary>
    /// Gets or sets a value indicating whether the letters are linked.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets how many letters the arrival letter is ahead of the departure letter, wrapping around the
    /// code range. When not set, half the code range is used (13 for A-Z).
    /// </summary>
    public int? Offset { get; set; }
}
