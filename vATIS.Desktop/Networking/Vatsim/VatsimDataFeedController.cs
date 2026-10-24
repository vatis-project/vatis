// <copyright file="VatsimDataFeedController.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Networking.Vatsim;

/// <summary>
/// A controller entry in the VATSIM data feed.
/// </summary>
public class VatsimDataFeedController
{
    /// <summary>
    /// Gets or sets the controller callsign.
    /// </summary>
    [JsonPropertyName("callsign")]
    public string? Callsign { get; set; }

    /// <summary>
    /// Gets or sets the facility type, e.g. zero for an observer and 4 for a tower.
    /// </summary>
    [JsonPropertyName("facility")]
    public int Facility { get; set; }
}
