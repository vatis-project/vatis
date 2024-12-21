using System;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;
public class Temperature : BaseFormat
{
    public Temperature()
    {
        Template = new()
        {
            Text = "{temp}",
            Voice = "TEMPERATURE {temp}"
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

    public Temperature Clone() => (Temperature)MemberwiseClone();
}