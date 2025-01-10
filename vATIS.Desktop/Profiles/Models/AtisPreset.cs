// <copyright file="AtisPreset.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a preset configuration for an ATIS station.
/// </summary>
public class AtisPreset
{
    private static readonly string[] ClosingVariables = ["[CLOSING]", "$CLOSING", "[CLOSING:VOX]", "$CLOSING:VOX"];

    /// <summary>
    /// Gets or sets the unique identifier for the ATIS preset.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the ordinal value for the ATIS preset, used for sorting and ordering purposes.
    /// </summary>
    public int? Ordinal { get; set; }

    /// <summary>
    /// Gets or sets the name of the ATIS preset.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the textual representation of airport conditions associated with the ATIS preset.
    /// </summary>
    public string? AirportConditions { get; set; }

    /// <summary>
    /// Gets or sets the NOTAMs (Notices to Airmen) associated with the ATIS preset.
    /// </summary>
    public string? Notams { get; set; }

    /// <summary>
    /// Gets or sets the template used for building the ATIS message.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Gets or sets the external generator configuration for ATIS data.
    /// </summary>
    public ExternalGenerator? ExternalGenerator { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the airport conditions data has been modified.
    /// </summary>
    [JsonIgnore]
    public bool IsAirportConditionsDirty { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the NOTAMs data has been modified.
    /// </summary>
    [JsonIgnore]
    public bool IsNotamsDirty { get; set; }

    /// <summary>
    /// Gets a value indicating whether the template contains any of the defined closing variables.
    /// </summary>
    [JsonIgnore]
    public bool HasClosingVariable =>
        this.Template is not null && ClosingVariables.Any(n => this.Template.Contains(n));

    /// <summary>
    /// Gets or sets arbitrary text information associated with the ATIS preset.
    /// </summary>
    [JsonIgnore]
    public string? ArbitraryText { get; set; }

    /// <inheritdoc/>
    public override string? ToString()
    {
        return this.Name;
    }

    /// <summary>
    /// Creates a copy of the current instance of the <see cref="AtisPreset"/> class.
    /// </summary>
    /// <returns>Returns a new <see cref="AtisPreset"/> object with the copied properties of the current instance.</returns>
    public AtisPreset Clone()
    {
        return new AtisPreset
        {
            Id = Guid.NewGuid(),
            Name = this.Name,
            AirportConditions = this.AirportConditions,
            Notams = this.Notams,
            Template = this.Template,
            ExternalGenerator = this.ExternalGenerator,
        };
    }
}
