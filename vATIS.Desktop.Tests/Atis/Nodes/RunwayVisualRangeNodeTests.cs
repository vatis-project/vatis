using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class RunwayVisualRangeNodeTests
{
    private const string Metar = "VVTS 011650Z 25009KT 0800 R25L/0600U FG 25/24 Q1015";

    private static string ParseVoice(string? spokenText)
    {
        var station = new AtisStation { Identifier = "VVTS", Name = "VVTS" };
        station.AtisFormat.RunwayVisualRange.SpokenText = spokenText;
        var node = new RunwayVisualRangeNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict(Metar));
        return node.VoiceAtis;
    }

    [Fact]
    public void DefaultSpokenText_IsRvr()
    {
        Assert.Contains("R-V-R", ParseVoice("R-V-R"));
    }

    [Fact]
    public void CustomSpokenText_ReplacesRvr()
    {
        var voice = ParseVoice("Runway Visual Range");
        Assert.Contains("Runway Visual Range", voice);
        Assert.DoesNotContain("R-V-R", voice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BlankSpokenText_OmitsLabel(string? spokenText)
    {
        var voice = ParseVoice(spokenText);
        Assert.DoesNotContain("R-V-R", voice);
    }

    private static string ParseText(string metar, string? tendencyOverride = null)
    {
        var station = new AtisStation { Identifier = "VVTS", Name = "VVTS" };
        if (tendencyOverride != null)
            station.AtisFormat.RunwayVisualRange.TendencyText["U"] = tendencyOverride;
        var node = new RunwayVisualRangeNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict(metar));
        return node.TextAtis;
    }

    [Fact]
    public void TendencyText_DefaultsToRawGroup()
    {
        Assert.Equal("R25L/0600U", ParseText(Metar));
    }

    [Fact]
    public void TendencyText_UsesCustomText_WithoutAffectingVoice()
    {
        Assert.Equal("R25L/0600UP", ParseText(Metar, "UP"));
        Assert.Contains("Going Up", ParseVoice("R-V-R"));
    }

    [Fact]
    public void TendencyText_NoTendency_LeavesRawGroup()
    {
        Assert.Equal("R25L/0600", ParseText("VVTS 011650Z 25009KT 0800 R25L/0600 FG 25/24 Q1015", "UP"));
    }
}
