namespace Vatsim.Vatis.Atis;

public class AtisVariable
{
    public AtisVariable(string find, string textReplace, string voiceReplace, string[]? aliases = null)
    {
        this.Find = find;
        this.TextReplace = textReplace;
        this.VoiceReplace = voiceReplace;
        this.Aliases = aliases;
    }

    public string Find { get; set; }

    public string TextReplace { get; set; }

    public string VoiceReplace { get; set; }

    public string[]? Aliases { get; set; }
}