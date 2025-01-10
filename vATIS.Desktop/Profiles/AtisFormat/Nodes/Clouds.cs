using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Converter;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class Clouds : BaseFormat
{
    public Clouds()
    {
        this.Template = new Template
        {
            Text = "{clouds}",
            Voice = "{clouds}"
        };
    }

    public bool IdentifyCeilingLayer { get; set; } = true;

    public bool ConvertToMetric { get; set; }

    public bool IsAltitudeInHundreds { get; set; }

    public UndeterminedLayer UndeterminedLayerAltitude { get; set; } = new("undetermined", "undetermined");

    [JsonConverter(typeof(CloudTypeConverter))]
    public Dictionary<string, CloudType> Types { get; set; } = new()
    {
        { "FEW", new CloudType("FEW{altitude}", "few clouds at {altitude}") },
        { "SCT", new CloudType("SCT{altitude}{convective}", "{altitude} scattered {convective}") },
        { "BKN", new CloudType("BKN{altitude}{convective}", "{altitude} broken {convective}") },
        { "OVC", new CloudType("OVC{altitude}{convective}", "{altitude} overcast {convective}") },
        { "VV", new CloudType("VV{altitude}", "indefinite ceiling {altitude}") },
        { "NSC", new CloudType("NSC", "no significant clouds") },
        { "NCD", new CloudType("NCD", "no clouds detected") },
        { "CLR", new CloudType("CLR", "sky clear below one-two thousand") },
        { "SKC", new CloudType("SKC", "sky clear") }
    };

    public Dictionary<string, string> ConvectiveTypes { get; set; } = new()
    {
        { "CB", "cumulonimbus" },
        { "TCU", "towering cumulus" }
    };

    public Clouds Clone()
    {
        return (Clouds)this.MemberwiseClone();
    }
}

public class UndeterminedLayer
{
    public UndeterminedLayer(string text, string voice)
    {
        this.Text = text;
        this.Voice = voice;
    }

    public string Text { get; set; }

    public string Voice { get; set; }
}

public class CloudType
{
    public CloudType(string text, string voice)
    {
        this.Text = text;
        this.Voice = voice;
    }

    public string Text { get; set; }

    public string Voice { get; set; }

    [JsonPropertyName("$type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [Obsolete("Do not use")]
    public string? Type => null;
}