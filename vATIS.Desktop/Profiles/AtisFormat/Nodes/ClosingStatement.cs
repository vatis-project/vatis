namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class ClosingStatement : BaseFormat
{
    public ClosingStatement()
    {
        this.Template = new Template
        {
            Text = "...ADVS YOU HAVE INFO {letter}.",
            Voice = "ADVISE ON INITIAL CONTACT, YOU HAVE INFORMATION {letter|word}"
        };
    }

    public bool AutoIncludeClosingStatement { get; set; } = true;

    public ClosingStatement Clone()
    {
        return (ClosingStatement)this.MemberwiseClone();
    }
}