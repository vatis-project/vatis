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
}
