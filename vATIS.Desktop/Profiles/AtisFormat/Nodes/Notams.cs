namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class Notams : BaseFormat
{
    public Notams()
    {
        this.Template = new Template
        {
            Text = "NOTAMS... {notams}",
            Voice = "NOTICES TO AIR MISSIONS: {notams}"
        };
    }

    public Notams Clone()
    {
        return (Notams)this.MemberwiseClone();
    }
}