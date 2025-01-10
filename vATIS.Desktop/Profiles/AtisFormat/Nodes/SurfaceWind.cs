using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

public class SurfaceWind
{
    public bool SpeakLeadingZero { get; set; }

    public MagneticVariationMeta MagneticVariation { get; set; } = new();

    public BaseFormat Standard { get; set; } = new()
    {
        Template = new Template
        {
            Text = "{wind_dir}{wind_spd}KT",
            Voice = "WIND {wind_dir} AT {wind_spd}"
        }
    };

    public BaseFormat StandardGust { get; set; } = new()
    {
        Template = new Template
        {
            Text = "{wind_dir}{wind_spd}G{wind_gust}KT",
            Voice = "WIND {wind_dir} AT {wind_spd} GUSTS {wind_gust}"
        }
    };

    public BaseFormat Variable { get; set; } = new()
    {
        Template = new Template
        {
            Text = "VRB{wind_spd}KT",
            Voice = "WIND VARIABLE AT {wind_spd}"
        }
    };

    public BaseFormat VariableGust { get; set; } = new()
    {
        Template = new Template
        {
            Text = "VRB{wind_spd}G{wind_gust}KT",
            Voice = "WIND VARIABLE AT {wind_spd} GUSTS {wind_gust}"
        }
    };

    public BaseFormat VariableDirection { get; set; } = new()
    {
        Template = new Template
        {
            Text = "{wind_vmin}V{wind_vmax}",
            Voice = "WIND VARIABLE BETWEEN {wind_vmin} AND {wind_vmax}"
        }
    };

    public CalmWind Calm { get; set; } = new();

    public SurfaceWind Clone()
    {
        return (SurfaceWind)this.MemberwiseClone();
    }
}

public class CalmWind : BaseFormat
{
    public CalmWind()
    {
        this.Template = new Template
        {
            Text = "{wind}",
            Voice = "WIND CALM"
        };
    }

    public int CalmWindSpeed { get; set; } = 2;

    public CalmWind Clone()
    {
        return (CalmWind)this.MemberwiseClone();
    }
}