using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class PrevailingVisibilityNodeTests
{
    private const string CavokMetar = "EDDG 011650Z 25009KT CAVOK 15/10 Q1015";

    private static PrevailingVisibilityNode CreateNode(string? cavokText = null, string? cavokVoice = null)
    {
        var station = new AtisStation { Identifier = "EDDG", Name = "EDDG" };
        if (cavokText != null)
            station.AtisFormat.Visibility.CavokText = cavokText;
        if (cavokVoice != null)
            station.AtisFormat.Visibility.CavokVoice = cavokVoice;
        return new PrevailingVisibilityNode { Station = station };
    }

    [Fact]
    public void Cavok_DefaultsToCavok()
    {
        var node = CreateNode();
        var visibility = new MetarDecoder().ParseNotStrict(CavokMetar).Visibility!;

        Assert.Equal("CAVOK", node.ParseTextVariables(visibility, "{visibility}"));
        Assert.Equal("CAVOK", node.ParseVoiceVariables(visibility, "{visibility}"));
    }

    [Fact]
    public void Cavok_UsesCustomText()
    {
        var node = CreateNode("CAVOK.", "clouds and visibility OK");
        var visibility = new MetarDecoder().ParseNotStrict(CavokMetar).Visibility!;

        Assert.Equal("CAVOK.", node.ParseTextVariables(visibility, "{visibility}"));
        Assert.Equal("clouds and visibility OK", node.ParseVoiceVariables(visibility, "{visibility}"));
    }
}
