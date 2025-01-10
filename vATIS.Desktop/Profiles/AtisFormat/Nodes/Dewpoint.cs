using System;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class Dewpoint : BaseFormat
{
    public Dewpoint()
    {
        this.Template = new Template
        {
            Text = "{dewpoint}",
            Voice = "DEWPOINT {dewpoint}"
        };
    }

    public bool UsePlusPrefix { get; set; }

    public bool SpeakLeadingZero { get; set; }

    [Obsolete("Use 'SpeakLeadingZero' instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool PronounceLeadingZero
    {
        get => false;
        set => this.SpeakLeadingZero = value;
    }

    public Dewpoint Clone()
    {
        return (Dewpoint)this.MemberwiseClone();
    }
}