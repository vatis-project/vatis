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
    private static readonly string[] s_closingVariables = ["[CLOSING]", "$CLOSING", "[CLOSING:VOX]", "$CLOSING:VOX"];

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
    /// Gets or sets the arrival runways this preset uses, separated by commas or spaces, e.g. <c>07L, 07R</c>.
    /// Airport conditions and NOTAMs with a runway condition are included or left out depending on them.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ArrivalRunways { get; set; }

    /// <summary>
    /// Gets or sets the departure runways this preset uses, separated by commas or spaces, e.g. <c>07L</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DepartureRunways { get; set; }

    /// <summary>
    /// Gets a value indicating whether the template contains any of the defined closing variables.
    /// </summary>
    [JsonIgnore]
    public bool HasClosingVariable =>
        Template is not null && s_closingVariables.Any(n => Template.Contains(n));

    /// <summary>
    /// Gets or sets arbitrary text information associated with the ATIS preset.
    /// </summary>
    [JsonIgnore]
    public string? ArbitraryText { get; set; }

    /// <inheritdoc/>
    public override string? ToString()
    {
        return Name;
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
            Name = Name,
            AirportConditions = AirportConditions,
            Notams = Notams,
            ArrivalRunways = ArrivalRunways,
            DepartureRunways = DepartureRunways,
            Template = Template,
            ExternalGenerator = ExternalGenerator,
        };
    }
}
