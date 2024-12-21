namespace Vatsim.Vatis.Atis;
public class AtisVariable
{
    public string Find { get; set; }
    public string TextReplace { get; set; }
    public string VoiceReplace { get; set; }
    public string[]? Aliases { get; set; }

    public AtisVariable(string find, string textReplace, string voiceReplace, string[]? aliases = null)
    {
        Find = find;
        TextReplace = textReplace;
        VoiceReplace = voiceReplace;
        Aliases = aliases;
    }
}