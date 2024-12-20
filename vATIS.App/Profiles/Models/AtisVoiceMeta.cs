namespace Vatsim.Vatis.Profiles.Models;

public class AtisVoiceMeta
{
    public bool UseTextToSpeech { get; set; } = true;
    public string? Voice { get; set; } = "Default";

    public AtisVoiceMeta Clone()
    {
        return new AtisVoiceMeta
        {
            UseTextToSpeech = UseTextToSpeech,
            Voice = Voice
        };
    }
}