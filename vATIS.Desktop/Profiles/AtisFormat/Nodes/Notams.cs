namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class Notams : BaseFormat
{
    public Notams()
    {
        Template = new()
        {
            Text = "NOTAMS... {notams}",
            Voice = "NOTICES TO AIR MISSIONS: {notams}"
        };
    }

    public Notams Clone() => (Notams)MemberwiseClone();
}