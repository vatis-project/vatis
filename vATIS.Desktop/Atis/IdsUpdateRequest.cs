// <copyright file="IdsUpdateRequest.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Represents a request to update an IDS.
/// </summary>
public class IdsUpdateRequest
{
    /// <summary>
    /// Gets or sets the ATIS station facility name.
    /// </summary>
    public string Facility { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ATIS preset.
    /// </summary>
    public string Preset { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ATIS letter.
    /// </summary>
    public string AtisLetter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the airport conditions.
    /// </summary>
    public string? AirportConditions { get; set; }

    /// <summary>
    /// Gets or sets the NOTAMs.
    /// </summary>
    public string? Notams { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the request.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the vATIS application version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the ATIS type.
    /// </summary>
    public string? AtisType { get; set; }
}
