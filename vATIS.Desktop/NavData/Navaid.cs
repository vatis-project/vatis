// <copyright file="Navaid.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;

namespace Vatsim.Vatis.NavData;

/// <summary>
/// Represents a navigational aid (Navaid) with an identifier and a name.
/// </summary>
public class Navaid
{
    /// <summary>
    /// Gets or sets the identifier of the Navaid.
    /// </summary>
    [JsonPropertyName("ID")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the Navaid.
    /// </summary>
    public required string Name { get; set; }
}
