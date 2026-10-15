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
        Assert.Equal(voice, node.VoiceAtis!.ToUpperInvariant());
    }

    [Fact]
    public void KnownHeight_IgnoresUndeterminedTemplate()
    {
        var node = Parse("FEW030");
        Assert.Equal("FEW030", node.TextAtis);
    }

    private static CloudNode ParseWith(string clouds, string voiceTemplate)
    {
        var station = new AtisStation { Identifier = "EPWA", Name = "EPWA" };
        station.AtisFormat.Clouds.Types["BKN"] = new CloudType("BKN{altitude}{convective}", voiceTemplate);
        station.AtisFormat.Clouds.Types["VV"] = new CloudType("VV{altitude}", "vertical visibility {altitude} {feet}");
        station.AtisFormat.Clouds.UndeterminedVerticalVisibility = new UndeterminedLayer("///", "not reported");
        var node = new CloudNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict($"EPWA 011650Z 25009KT 9999 {clouds} 05/02 Q1015"));
        return node;
    }

    [Fact]
    public void FeetToken_KnownHeight_IsSpoken()
    {
        var node = ParseWith("BKN030", "{altitude} {feet} broken");
        Assert.Contains("THREE THOUSAND  FEET BROKEN", node.VoiceAtis!.ToUpperInvariant());
    }

    [Fact]
    public void VerticalVisibility_UsesOwnUndeterminedPhrase()
    {
        var node = ParseWith("VV///", "{altitude} {feet} broken");
        Assert.Equal("VERTICAL VISIBILITY  NOT REPORTED", node.VoiceAtis!.ToUpperInvariant());
    }

    [Fact]
    public void LegacyProfile_GetsUndeterminedTypeAdded()
    {
        var clouds = JsonSerializer.Deserialize<Clouds>("""{"Types":{"FEW":"few","SCT":"sct","BKN":"bkn","OVC":"ovc","VV":"vv","NSC":"n","NCD":"n","CLR":"c","SKC":"s"}}""");
        Assert.NotNull(clouds);
        Assert.True(clouds!.Types.ContainsKey("UND"));
    }

    [Fact]
    public void ConvectiveText_DefaultsToAcronym_AndIsCustomizable()
    {
        var station = new AtisStation { Identifier = "EPWA", Name = "EPWA" };
        const string metar = "EPWA 011650Z 25009KT 9999 BKN030CB 05/02 Q1015";

        var node = new CloudNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict(metar));
        Assert.Equal("BKN030CB", node.TextAtis);

        station.AtisFormat.Clouds.ConvectiveTypesText["CB"] = "CUMULONIMBUS";
        node = new CloudNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict(metar));
        Assert.Equal("BKN030CUMULONIMBUS", node.TextAtis);
        Assert.Contains("CUMULONIMBUS", node.VoiceAtis!.ToUpperInvariant());
    }

    [Fact]
    public void AltitudeFormat_TextAndVoiceAreIndependent()
    {
        var station = new AtisStation { Identifier = "EPWA", Name = "EPWA" };
        const string metar = "EPWA 011650Z 25009KT 9999 FEW030 05/02 Q1015";

        var node = new CloudNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict(metar));
        Assert.Equal("FEW030", node.TextAtis);
        var defaultVoice = node.VoiceAtis;

        station.AtisFormat.Clouds.AltitudeFormat.Text.Suffix = "FT";
        station.AtisFormat.Clouds.AltitudeFormat.Voice.Style = Vatsim.Vatis.Profiles.AtisFormat.Nodes.NumberStyle.Digits;
        node = new CloudNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict(metar));
        Assert.Equal("FEW030FT", node.TextAtis);
        Assert.NotEqual(defaultVoice, node.VoiceAtis);
        Assert.Contains("30", node.VoiceAtis);
        Assert.DoesNotContain("FT", node.VoiceAtis);
    }
}
