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
        return node.VoiceAtis!;
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
        return node.TextAtis!;
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

    private static string Voice(string metar, System.Action<Vatsim.Vatis.Profiles.AtisFormat.Nodes.RunwayVisualRange>? configure = null)
    {
        var station = new AtisStation { Identifier = "VVTS", Name = "VVTS" };
        configure?.Invoke(station.AtisFormat.RunwayVisualRange);
        var node = new RunwayVisualRangeNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict(metar));
        return System.Text.RegularExpressions.Regex.Replace(node.VoiceAtis!, @"\s+", " ");
    }

    [Theory]
    [InlineData("R25L/M0600", "less than six hundred")]
    [InlineData("R25L/P1500", "more than one thousand five hundred")]
    [InlineData("R25L/0400V0800", "variable between four hundred and eight hundred")]
    [InlineData("R25L/M0400V0800", "variable from less than four hundred to eight hundred")]
    [InlineData("R25L/0400VP2000", "four hundred variable to greater than two thousand")]
    [InlineData("R25L/M0400VP2000", "variable from less than four hundred to greater than two thousand")]
    public void VoicePhrases_Defaults_ReproduceExistingWording(string group, string expected)
    {
        var voice = Voice($"VVTS 011650Z 25009KT 0800 {group} FG 25/24 Q1015");
        Assert.Contains(expected, voice, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VoicePhrases_CustomPhrasesAreUsed()
    {
        var voice = Voice("VVTS 011650Z 25009KT 0800 R25L/0400V0800 FG 25/24 Q1015", f =>
        {
            f.VoicePhrases["Variable"] = "{min} to {max}";
        });
        Assert.Contains("four hundred to eight hundred", voice, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void VoicePhrases_MissingKeyFallsBackToDefault()
    {
        var voice = Voice("VVTS 011650Z 25009KT 0800 R25L/M0600 FG 25/24 Q1015", f => f.VoicePhrases.Clear());
        Assert.Contains("less than", voice);
    }
}
