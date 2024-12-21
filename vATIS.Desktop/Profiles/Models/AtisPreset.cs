using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.Models;

public class AtisPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? Ordinal { get; set; }
    public string? Name { get; set; }
    public string? AirportConditions { get; set; }
    public string? Notams { get; set; }
    public string? Template { get; set; }
    public ExternalGenerator? ExternalGenerator { get; set; } = new();

    [JsonIgnore] public bool IsAirportConditionsDirty { get; set; }

    [JsonIgnore] public bool IsNotamsDirty { get; set; }
    
    [JsonIgnore]
    public bool HasClosingVariable => Template is not null && ClosingVariables.Any(n => Template.Contains(n));

    public override string? ToString() => Name;

    // Legacy
    [JsonIgnore] public string? ArbitraryText { get; set; }

    private static readonly string[] ClosingVariables = ["[CLOSING]", "$CLOSING", "[CLOSING:VOX]", "$CLOSING:VOX"];

    public AtisPreset Clone()
    {
        return new AtisPreset
        {
            Id = Guid.NewGuid(),
            Name = Name,
            AirportConditions = AirportConditions,
            Notams = Notams,
            Template = Template,
            ExternalGenerator = ExternalGenerator
        };
    }
}