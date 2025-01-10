using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ReactiveUI;
using Slugify;

namespace Vatsim.Vatis.Profiles.Models;

public class AtisStation : ReactiveObject
{
    private List<ContractionMeta> _contractionMetas = [];

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public required string Identifier { get; set; }

    public required string Name { get; set; }

    public AtisType AtisType { get; set; }

    public CodeRangeMeta CodeRange { get; set; } = new('A', 'Z');

    public AtisFormat.AtisFormat AtisFormat { get; set; } = new();

    public bool NotamsBeforeFreeText { get; set; }

    public bool AirportConditionsBeforeFreeText { get; set; }

    public uint Frequency { get; set; }

    public string? IdsEndpoint { get; set; }

    public bool UseDecimalTerminology { get; set; }

    public AtisVoiceMeta AtisVoice { get; set; } = new();

    public List<AtisPreset> Presets { get; set; } = [];

    public List<ContractionMeta> Contractions
    {
        get => this._contractionMetas;
        set
        {
            this._contractionMetas = [];
            foreach (var contractionMeta in value)
            {
                if (string.IsNullOrEmpty(contractionMeta.VariableName) && !string.IsNullOrEmpty(contractionMeta.Text))
                {
                    var slug = new SlugHelper().GenerateSlug(contractionMeta.Text);
                    slug = slug.Replace("-", "_").ToUpperInvariant();
                    contractionMeta.VariableName = slug;
                }

                this._contractionMetas.Add(contractionMeta);
            }
        }
    }

    public List<StaticDefinition> AirportConditionDefinitions { get; set; } = [];

    public List<StaticDefinition> NotamDefinitions { get; set; } = [];

    [JsonIgnore] public bool IsFaaAtis => this.Identifier.StartsWith('K') || this.Identifier.StartsWith('P');

    [JsonIgnore] public string? TextAtis { get; set; }

    [JsonIgnore] public char AtisLetter { get; set; }

    // Legacy
    [Obsolete("Use 'Frequency' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint AtisFrequency
    {
        get => 0;
        set => this.Frequency = value < 100000000 ? (value + 100000) * 1000 : value;
    }

    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public LegacyObservationTime? ObservationTime
    {
        get => null;
        set
        {
            if (value is not null)
            {
                this.AtisFormat.ObservationTime.StandardUpdateTime = [value.Time];
            }
        }
    }

    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public LegacyMagneticVariation? MagneticVariation
    {
        get => null;
        set
        {
            if (value is not null)
            {
                this.AtisFormat.SurfaceWind.MagneticVariation.Enabled = value.Enabled;
                this.AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = value.MagneticDegrees;
            }
        }
    }

    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<LegacyTransitionLevel>? TransitionLevels { get; set; }

    public override string ToString()
    {
        return this.AtisType != AtisType.Combined
            ? $"{this.Name} ({this.Identifier}) {this.AtisType}"
            : $"{this.Name} ({this.Identifier})";
    }

    public AtisStation Clone()
    {
        return new AtisStation
        {
            Id = Guid.NewGuid().ToString(),
            Identifier = this.Identifier,
            Name = this.Name,
            AtisType = this.AtisType,
            CodeRange = this.CodeRange,
            AtisFormat = this.AtisFormat.Clone(),
            NotamsBeforeFreeText = this.NotamsBeforeFreeText,
            AirportConditionsBeforeFreeText = this.AirportConditionsBeforeFreeText,
            Frequency = this.Frequency,
            IdsEndpoint = this.IdsEndpoint,
            UseDecimalTerminology = this.UseDecimalTerminology,
            AtisVoice = this.AtisVoice.Clone(),
            Presets = this.Presets.Select(x => x.Clone()).ToList(),
            Contractions = this.Contractions.Select(x => x.Clone()).ToList(),
            AirportConditionDefinitions = this.AirportConditionDefinitions.Select(x => x.Clone()).ToList(),
            NotamDefinitions = this.NotamDefinitions.Select(x => x.Clone()).ToList()
        };
    }

    [Obsolete]
    public class LegacyTransitionLevel
    {
        public int Low { get; set; }

        public int High { get; set; }

        public int Altitude { get; set; }
    }

    [Obsolete]
    public class LegacyMagneticVariation
    {
        public bool Enabled { get; set; }

        public int MagneticDegrees { get; set; }
    }

    [Obsolete]
    public class LegacyObservationTime
    {
        public bool Enabled { get; set; }

        public int Time { get; set; }
    }
}