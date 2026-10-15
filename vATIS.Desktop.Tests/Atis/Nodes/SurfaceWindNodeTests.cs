using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class SurfaceWindNodeTests
{
    private const string Metar = "KJFK 011651Z 25009G15KT 10SM CLR 20/10 A3000";

    private const string MpsMetar = "UUEE 011630Z 25004G09MPS 9999 NSC 20/10 Q1015";

    private static string ParseText(string template, string metar = Metar)
    {
        var node = new SurfaceWindNode { Station = new AtisStation { Identifier = "KJFK", Name = "KJFK" } };
        var wind = new MetarDecoder().ParseNotStrict(metar).SurfaceWind!;
        return node.ParseTextVariables(wind, template);
    }

    private static string ParseVoice(string template, string metar = Metar)
    {
        var node = new SurfaceWindNode { Station = new AtisStation { Identifier = "KJFK", Name = "KJFK" } };
        var wind = new MetarDecoder().ParseNotStrict(metar).SurfaceWind!;
        return node.ParseVoiceVariables(wind, template);
    }

    [Theory]
    [InlineData("{wind_spd}", "09")]
    [InlineData("{wind_spd:#}", "9")]
    [InlineData("{wind_spd:##}", "09")]
    [InlineData("{wind_spd:###}", "009")]
    [InlineData("{wind_gust:#}", "15")]
    [InlineData("{wind_spd|kt:#}", "9")]
    [InlineData("{wind_gust|kt:#}", "15")]
    [InlineData("{wind_dir}{wind_spd:#}G{wind_gust:#}KT", "2509G15KT")]
    public void ParseTextVariables_SpeedFormat(string template, string expected)
    {
        Assert.Equal(expected, ParseText(template));
    }

    [Theory]
    [InlineData("{wind_spd}", "04")]
    [InlineData("{wind_spd:#}", "4")]
    [InlineData("{wind_gust:#}", "9")]
    [InlineData("{wind_spd|mps}", "04")]
    [InlineData("{wind_spd|mps:#}", "4")]
    [InlineData("{wind_gust|mps:#}", "9")]
    [InlineData("{wind_spd|kt:#}", "7")]
    [InlineData("{wind_gust|kt:#}", "17")]
    public void ParseTextVariables_SpeedFormat_Mps(string template, string expected)
    {
        Assert.Equal(expected, ParseText(template, MpsMetar));
    }

    [Fact]
    public void ParseVoiceVariables_IgnoresDigitSuffix()
    {
        var result = ParseVoice("{wind_spd:#}");

        Assert.DoesNotContain("{", result);
        Assert.DoesNotContain(":", result);
    }

    private static SurfaceWindNode Node(System.Action<Vatsim.Vatis.Profiles.AtisFormat.Nodes.SurfaceWind> configure)
    {
        var station = new AtisStation { Identifier = "KJFK", Name = "KJFK" };
        configure(station.AtisFormat.SurfaceWind);
        return new SurfaceWindNode { Station = station };
    }

    [Fact]
    public void VariableFormats_Default_IsUnchanged()
    {
        var node = Node(_ => { });
        var wind = new MetarDecoder().ParseNotStrict(Metar).SurfaceWind!;
        Assert.Equal("25009G15KT", node.ParseTextVariables(wind, "{wind_dir}{wind_spd}G{wind_gust}KT"));
        Assert.Equal("two five zero at niner gusts one five",
            node.ParseVoiceVariables(wind, "{wind_dir} at {wind_spd} gusts {wind_gust}"), ignoreCase: true);
    }

    [Fact]
    public void VariableFormats_TextAndVoiceAreIndependent()
    {
        var node = Node(w =>
        {
            w.VariableFormats["wind_spd"].Text.Suffix = "/";
            w.VariableFormats["wind_spd"].Voice.Suffix = " knots";
            w.VariableFormats["wind_dir"].Voice.Style = Vatsim.Vatis.Profiles.AtisFormat.Nodes.NumberStyle.Digits;
        });
        var wind = new MetarDecoder().ParseNotStrict(Metar).SurfaceWind!;
        Assert.Equal("250/09/KT", node.ParseTextVariables(wind, "{wind_dir}/{wind_spd}KT"));
        Assert.Equal("250 at niner knots", node.ParseVoiceVariables(wind, "{wind_dir} at {wind_spd}"), ignoreCase: true);
    }

    [Fact]
    public void VariableFormats_UnitVariantsShareBaseKey()
    {
        var node = Node(w => w.VariableFormats["wind_spd"].Text.Suffix = "!");
        var wind = new MetarDecoder().ParseNotStrict(MpsMetar).SurfaceWind!;
        Assert.Equal("04!", node.ParseTextVariables(wind, "{wind_spd}"));
        Assert.Contains("!", node.ParseTextVariables(wind, "{wind_spd|kt}"));
    }
}
