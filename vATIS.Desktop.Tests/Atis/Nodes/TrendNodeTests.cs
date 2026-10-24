using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class TrendNodeTests
{
    private const string NosigMetar = "EKBI 021520Z 24010KT 9999 NSC 12/08 Q1015 NOSIG";
    private const string NoTrendMetar = "EKBI 021520Z 24010KT 9999 NSC 12/08 Q1015";

    private static TrendNode Parse(string metar, AtisStation? station = null)
    {
        var node = new TrendNode
        {
            Station = station ?? new AtisStation { Identifier = "EKBI", Name = "EKBI" },
        };
        node.Parse(new MetarDecoder().ParseNotStrict(metar));
        return node;
    }

    [Fact]
    public void Nosig_UsesDefaultTemplates()
    {
        var node = Parse(NosigMetar);
        Assert.Equal("NOSIG", node.TextAtis);
        Assert.Equal("TREND, No Significant Changes", node.VoiceAtis);
    }

    [Fact]
    public void Nosig_UsesConfiguredTemplates()
    {
        var station = new AtisStation { Identifier = "EKBI", Name = "EKBI" };
        station.AtisFormat.Trend.TemplateText = "TREND {Trend}";
        station.AtisFormat.Trend.TemplateVoice = "Forecast: {trend}";

        var node = Parse(NosigMetar, station);
        Assert.Equal("TREND NOSIG", node.TextAtis);
        Assert.Equal("Forecast: No Significant Changes", node.VoiceAtis);
    }

    [Fact]
    public void Nosig_VoiceTemplateWithoutPrefixOmitsTrendWord()
    {
        var station = new AtisStation { Identifier = "EKBI", Name = "EKBI" };
        station.AtisFormat.Trend.TemplateVoice = "{Trend}";

        var node = Parse(NosigMetar, station);
        Assert.Equal("No Significant Changes", node.VoiceAtis);
    }

    [Fact]
    public void NoTrend_IgnoresTemplates()
    {
        var station = new AtisStation { Identifier = "EKBI", Name = "EKBI" };
        station.AtisFormat.Trend.NotAvailableText = "NO TREND";
        station.AtisFormat.Trend.NotAvailableVoice = "Trend not available";

        var node = Parse(NoTrendMetar, station);
        Assert.Equal("NO TREND", node.TextAtis);
        Assert.Equal("Trend not available", node.VoiceAtis);
    }
}
