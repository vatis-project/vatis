using System.Text.Json.Serialization;
using System;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;
public class Dewpoint : BaseFormat
{
    public Dewpoint()
    {
        Template = new()
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
        get => default;
        set => SpeakLeadingZero = value;
    }

    public Dewpoint Clone() => (Dewpoint)MemberwiseClone();
}
