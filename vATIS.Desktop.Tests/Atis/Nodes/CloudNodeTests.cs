using System.Text.Json;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class CloudNodeTests
{
    private static CloudNode Parse(string clouds)
    {
        var node = new CloudNode { Station = new AtisStation { Identifier = "EPWA", Name = "EPWA" } };
        node.Parse(new MetarDecoder().ParseNotStrict($"EPWA 011650Z 25009KT 9999 {clouds} 05/02 Q1015"));
        return node;
    }

    [Theory]
    [InlineData("FEW///", "FEW///", "FEW CLOUD HEIGHT NOT AVAILABLE")]
    [InlineData("BKN///TCU", "BKN///TCU", "BROKEN CLOUD HEIGHT NOT AVAILABLE TOWERING CUMULUS")]
    public void UndeterminedHeight_UsesUndeterminedTemplate(string metar, string text, string voice)
    {
        var node = Parse(metar);
        Assert.Equal(text, node.TextAtis);
        Assert.Equal(voice, node.VoiceAtis.ToUpperInvariant());
    }

    [Fact]
    public void KnownHeight_IgnoresUndeterminedTemplate()
    {
        var node = Parse("FEW030");
        Assert.Equal("FEW030", node.TextAtis);
    }

    [Fact]
    public void LegacyProfile_GetsUndeterminedTypeAdded()
    {
        var clouds = JsonSerializer.Deserialize<Clouds>("""{"Types":{"FEW":"few","SCT":"sct","BKN":"bkn","OVC":"ovc","VV":"vv","NSC":"n","NCD":"n","CLR":"c","SKC":"s"}}""");
        Assert.NotNull(clouds);
        Assert.True(clouds!.Types.ContainsKey("UND"));
    }
}
