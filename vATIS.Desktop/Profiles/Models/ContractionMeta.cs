using System;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.Models;

public class ContractionMeta
{
    public string? VariableName { get; set; }

    public string? Text { get; set; }

    public string? Voice { get; set; }

    [Obsolete("Use Text instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? String
    {
        get => null;
        set => this.Text = value ?? string.Empty;
    }

    [Obsolete("Use Voice instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Spoken
    {
        get => null;
        set => this.Voice = value ?? string.Empty;
    }

    public ContractionMeta Clone()
    {
        return new ContractionMeta
        {
            Text = this.Text,
            Voice = this.Voice
        };
    }
}