// <copyright file="Airport.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;

namespace Vatsim.Vatis.NavData;

/// <summary>
/// Represents an airport with its basic information.
/// </summary>
public class Airport
{
    /// <summary>
    /// Gets or sets the unique identifier of the airport.
    /// </summary>
    [JsonPropertyName("ID")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the airport.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the latitude coordinate of the airport.
    /// </summary>
    [JsonPropertyName("Lat")]
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude coordinate of the airport.
    /// </summary>
    [JsonPropertyName("Lon")]
    public double Longitude { get; set; }
}
