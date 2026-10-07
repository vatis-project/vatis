using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class AutoObservationNodeTests
{
    private const string AutoMetar = "EKBI 021520Z AUTO 24010KT 9999 NSC 12/08 Q1015";
    private const string ManualMetar = "EKBI 021520Z 24010KT 9999 NSC 12/08 Q1015";

    private static AutoObservationNode Parse(string metar, AtisStation? station = null)
    {
        var node = new AutoObservationNode
        {
            Station = station ?? new AtisStation { Identifier = "EKBI", Name = "EKBI" },
        };
        node.Parse(new MetarDecoder().ParseNotStrict(metar));
        return node;
    }

    [Fact]
    public void AutoMetar_UsesDefaultTemplates()
    {
        var node = Parse(AutoMetar);
        Assert.Equal("AUTO", node.TextAtis);
        Assert.Equal("Automatic Observation", node.VoiceAtis);
    }

    [Fact]
    public void AutoMetar_UsesConfiguredTemplates()
    {
        var station = new AtisStation { Identifier = "EKBI", Name = "EKBI" };
        station.AtisFormat.AutoObservation.TextTemplate = "AUTOMATIC";
        station.AtisFormat.AutoObservation.VoiceTemplate = "Automatisk observation";

        var node = Parse(AutoMetar, station);
        Assert.Equal("AUTOMATIC", node.TextAtis);
        Assert.Equal("Automatisk observation", node.VoiceAtis);
    }

    [Fact]
    public void ManualMetar_ProducesNothing()
    {
        var node = Parse(ManualMetar);
        Assert.True(string.IsNullOrEmpty(node.TextAtis));
        Assert.True(string.IsNullOrEmpty(node.VoiceAtis));
    }
}
