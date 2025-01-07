using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using ReactiveUI;
using Slugify;

namespace Vatsim.Vatis.Profiles.Models;

public class AtisStation : ReactiveObject
{
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

    private List<ContractionMeta> mContractionMetas = [];
    public List<ContractionMeta> Contractions
    {
        get => mContractionMetas;
        set
        {
            mContractionMetas = [];
            foreach (var contractionMeta in value)
            {
                if (string.IsNullOrEmpty(contractionMeta.VariableName) && !string.IsNullOrEmpty(contractionMeta.Text))
                {
                    var slug = new SlugHelper().GenerateSlug(contractionMeta.Text);
                    slug = slug.Replace("-", "_").ToUpperInvariant();
                    contractionMeta.VariableName = slug;
                }
                mContractionMetas.Add(contractionMeta);
            }
        }
    }
    public List<StaticDefinition> AirportConditionDefinitions { get; set; } = [];
    public List<StaticDefinition> NotamDefinitions { get; set; } = [];

    public override string ToString() =>
        AtisType != AtisType.Combined ? $"{Name} ({Identifier}) {AtisType}" : $"{Name} ({Identifier})";

    [JsonIgnore] public bool IsFaaAtis => (Identifier.StartsWith('K') || Identifier.StartsWith('P'));
    [JsonIgnore] public string? TextAtis { get; set; }
    [JsonIgnore] public string? AtisLetter { get; set; }

    // Legacy
    [Obsolete("Use 'Frequency' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint AtisFrequency
    {
        get => default;
        set => Frequency = value < 100000000 ? (value + 100000) * 1000 : value;
    }

    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public LegacyObservationTime? ObservationTime
    {
        get => default;
        set
        {
            if (value is not null)
            {
                AtisFormat.ObservationTime.StandardUpdateTime = [value.Time];
            }
        }
    }

    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public LegacyMagneticVariation? MagneticVariation
    {
        get => default;
        set
        {
            if (value is not null)
            {
                AtisFormat.SurfaceWind.MagneticVariation.Enabled = value.Enabled;
                AtisFormat.SurfaceWind.MagneticVariation.MagneticDegrees = value.MagneticDegrees;
            }
        }
    }

    [Obsolete("Use 'AtisFormat' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<LegacyTransitionLevel>? TransitionLevels { get; set; }

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
}