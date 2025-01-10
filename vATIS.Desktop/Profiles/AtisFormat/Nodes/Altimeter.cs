namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class Altimeter : BaseFormat
{
    public Altimeter()
    {
        this.Template = new Template
        {
            Text = "A{altimeter} ({altimeter|text})",
            Voice = "ALTIMETER {altimeter}"
        };
    }

    public bool PronounceDecimal { get; set; }

    public Altimeter Clone()
    {
        return (Altimeter)this.MemberwiseClone();
    }
}