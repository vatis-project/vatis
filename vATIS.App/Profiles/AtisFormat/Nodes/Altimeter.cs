namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;
public class Altimeter : BaseFormat
{
    public Altimeter()
    {
        Template = new()
        {
            Text = "A{altimeter} ({altimeter|text})",
            Voice = "ALTIMETER {altimeter}"
        };
    }

    public bool PronounceDecimal { get; set; }

    public Altimeter Clone() => (Altimeter)MemberwiseClone();
}
