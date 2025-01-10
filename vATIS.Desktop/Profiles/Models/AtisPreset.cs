using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.Models;

public class AtisPreset
{
    private static readonly string[] s_closingVariables = ["[CLOSING]", "$CLOSING", "[CLOSING:VOX]", "$CLOSING:VOX"];

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
    public bool HasClosingVariable =>
        this.Template is not null && s_closingVariables.Any(n => this.Template.Contains(n));

    // Legacy
    [JsonIgnore] public string? ArbitraryText { get; set; }

    public override string? ToString()
    {
        return this.Name;
    }

    public AtisPreset Clone()
    {
        return new AtisPreset
        {
            Id = Guid.NewGuid(),
            Name = this.Name,
            AirportConditions = this.AirportConditions,
            Notams = this.Notams,
            Template = this.Template,
            ExternalGenerator = this.ExternalGenerator
        };
    }
}