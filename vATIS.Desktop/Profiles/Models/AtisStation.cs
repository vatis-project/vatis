// <copyright file="AtisStation.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ReactiveUI;
using Slugify;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents an ATIS station and its configuration.
/// </summary>
public class AtisStation : ReactiveObject
{
    private List<ContractionMeta> _contractionMetas = [];

    /// <summary>
    /// Gets or sets the unique identifier of the ATIS station.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the identifier of the ATIS station.
    /// </summary>
    public required string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the name of the ATIS station.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the type of the ATIS station.
    /// </summary>
    public AtisType AtisType { get; set; }

    /// <summary>
    /// Gets or sets the allowable range of letters used in the ATIS station.
    /// </summary>
    public CodeRangeMeta CodeRange { get; set; } = new('A', 'Z');

    /// <summary>
    /// Gets or sets the format configurations for the ATIS station.
    /// </summary>
    public AtisFormat.AtisFormat AtisFormat { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether NOTAMs should appear before free text in the ATIS message.
    /// </summary>
    public bool NotamsBeforeFreeText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether airport conditions are included before the free text section in the ATIS message.
    /// </summary>
    public bool AirportConditionsBeforeFreeText { get; set; }

    /// <summary>
    /// Gets or sets the frequency of the ATIS station.
    /// </summary>
    public uint Frequency { get; set; }

    /// <summary>
    /// Gets or sets the IDS Endpoint associated with the ATIS station.
    /// </summary>
    public string? IdsEndpoint { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether decimal terminology should be used in the ATIS station.
    /// </summary>
    public bool UseDecimalTerminology { get; set; }

    /// <summary>
    /// Gets or sets the metadata related to the ATIS voice configuration.
    /// </summary>
    public AtisVoiceMeta AtisVoice { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of ATIS presets associated with the station.
    /// </summary>
    public List<AtisPreset> Presets { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of contraction metadata associated with the ATIS station.
    /// </summary>
    public List<ContractionMeta> Contractions
    {
        get => _contractionMetas;
        set
        {
            _contractionMetas = [];
            foreach (var contractionMeta in value)
            {
                if (string.IsNullOrEmpty(contractionMeta.VariableName) && !string.IsNullOrEmpty(contractionMeta.Text))
                {
                    var slug = new SlugHelper().GenerateSlug(contractionMeta.Text);
                    slug = slug.Replace("-", "_").ToUpperInvariant();
                    contractionMeta.VariableName = slug;
                }

                _contractionMetas.Add(contractionMeta);
            }
        }
    }

    /// <summary>
    /// Gets or sets the list of static definitions that describe airport-specific conditions for the ATIS station.
    /// </summary>
    public List<StaticDefinition> AirportConditionDefinitions { get; set; } = [];

    /// <summary>
    /// Gets or sets the collection of static NOTAM (Notice to Airmen) definitions.
    /// </summary>
    public List<StaticDefinition> NotamDefinitions { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether the ATIS station is an FAA ATIS station.
    /// </summary>
    [JsonIgnore]
    public bool IsFaaAtis => Identifier.StartsWith('K') || Identifier.StartsWith('P');

    /// <summary>
    /// Gets or sets the textual broadcast content of the ATIS station.
    /// </summary>
    [JsonIgnore]
    public string? TextAtis { get; set; }

    /// <summary>
    /// Gets or sets the current ATIS letter for the station.
    /// </summary>
    [JsonIgnore]
    public char AtisLetter { get; set; }

    /// <summary>
    /// Gets or sets the ATIS frequency of the station.
    /// </summary>
    /// <remarks>
    /// This property is marked as obsolete. Use <see cref="Frequency"/> instead.
    /// </remarks>
    [Obsolete("Use 'Frequency' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint AtisFrequency
    {
        get => 0;
        set => Frequency = value < 100000000 ? (value + 100000) * 1000 : value;
    }

    /// <summary>
    /// Gets or sets the observation time of the ATIS station.
    /// </summary>
    /// <remarks>
    /// This property is marked as obsolete. Use <see cref="AtisFormat"/> instead.
    /// </remarks>
    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public LegacyObservationTime? ObservationTime
    {
        get => null;
        set
        {
            if (value is not null)
            {
                AtisFormat.ObservationTime.StandardUpdateTime = [value.Time];
            }
        }
    }

    /// <summary>
    /// Gets or sets the magnetic variation for the ATIS station.
    /// </summary>
    /// <remarks>
    /// This property is obsolete; use <see cref="AtisFormat"/> instead.
    /// </remarks>
    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public LegacyMagneticVariation? MagneticVariation
    {
        get => null;
        set
        {
            if (value is not null)
            {
                AtisFormat.SurfaceWind.MagneticVariation.Enabled = value.Enabled;
                AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = value.MagneticDegrees;
            }
        }
    }

    /// <summary>
    /// Gets or sets the legacy transition levels for the ATIS station.
    /// </summary>
    /// <remarks>
    /// This property is marked as obsolete. Use <see cref="AtisFormat"/> instead.
    /// </remarks>
    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<LegacyTransitionLevel>? TransitionLevels { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return AtisType != AtisType.Combined
            ? $"{Name} ({Identifier}) {AtisType}"
            : $"{Name} ({Identifier})";
    }

    /// <summary>
    /// Creates a deep copy of the current <see cref="AtisStation"/> instance.
    /// </summary>
    /// <returns>A new <see cref="AtisStation"/> instance that is a deep copy of the current instance.</returns>
    public AtisStation Clone()
    {
        return new AtisStation
        {
            Id = Guid.NewGuid().ToString(),
            Identifier = Identifier,
            Name = Name,
            AtisType = AtisType,
            CodeRange = CodeRange,
            AtisFormat = AtisFormat.Clone(),
            NotamsBeforeFreeText = NotamsBeforeFreeText,
            AirportConditionsBeforeFreeText = AirportConditionsBeforeFreeText,
            Frequency = Frequency,
            IdsEndpoint = IdsEndpoint,
            UseDecimalTerminology = UseDecimalTerminology,
            AtisVoice = AtisVoice.Clone(),
            Presets = Presets.Select(x => x.Clone()).ToList(),
            Contractions = Contractions.Select(x => x.Clone()).ToList(),
            AirportConditionDefinitions = AirportConditionDefinitions.Select(x => x.Clone()).ToList(),
            NotamDefinitions = NotamDefinitions.Select(x => x.Clone()).ToList(),
        };
    }

    /// <summary>
    /// Represents a legacy configuration for transition levels.
    /// </summary>
    /// <remarks>This property is obsolete.</remarks>
    [Obsolete]
    public class LegacyTransitionLevel
    {
        /// <summary>
        /// Gets or sets the lower boundary of the legacy transition level.
        /// </summary>
        public int Low { get; set; }

        /// <summary>
        /// Gets or sets the high altitude value.
        /// </summary>
        public int High { get; set; }

        /// <summary>
        /// Gets or sets the altitude value for the legacy transition level.
        /// </summary>
        public int Altitude { get; set; }
    }

    /// <summary>
    /// Represents the legacy magnetic variation settings.
    /// </summary>
    /// <remarks>This property is obsolete.</remarks>
    [Obsolete]
    public class LegacyMagneticVariation
    {
        /// <summary>
        /// Gets or sets a value indicating whether the variation is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the magnetic degrees for magnetic variation.
        /// </summary>
        public int MagneticDegrees { get; set; }
    }

    /// <summary>
    /// Represents the legacy observation time settings.
    /// </summary>
    /// <remarks>This property is obsolete.</remarks>
    [Obsolete]
    public class LegacyObservationTime
    {
        /// <summary>
        /// Gets or sets a value indicating whether this feature is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the time for the legacy observation settings.
        /// </summary>
        public int Time { get; set; }
    }
}
