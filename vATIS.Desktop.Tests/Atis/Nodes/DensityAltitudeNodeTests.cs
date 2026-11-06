using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class DensityAltitudeNodeTests
{
    private static DensityAltitudeNode Parse(int altitude, NumberStyle textStyle, NumberStyle voiceStyle,
        string voiceSuffix = "", string textSuffix = "")
    {
        var station = new AtisStation { Identifier = "CYHM", Name = "CYHM" };
        var format = station.AtisFormat.DensityAltitude;
        format.Format.Text.Style = textStyle;
        format.Format.Voice.Style = voiceStyle;
        format.Format.Voice.Suffix = voiceSuffix;
        format.Format.Text.Suffix = textSuffix;

        var node = new DensityAltitudeNode { Station = station };
        node.Parse(new MetarDecoder().ParseNotStrict($"CYHM 052100Z 24010KT 15SM FEW030 14/08 A2975 RMK DENSITY ALT {altitude}FT"));
        return node;
    }

    [Fact]
    public void Parse_Default_UsesDigitsAndSerialWords()
    {
        var node = Parse(1000, NumberStyle.Default, NumberStyle.Default);

        Assert.Equal("DENSITY ALTITUDE 1000FT", node.TextAtis);
        Assert.Equal("Density altitude one zero zero zero feet", node.VoiceAtis);
    }

    [Fact]
    public void Parse_GroupStyle_UsesGroupWords()
    {
        var node = Parse(1100, NumberStyle.Group, NumberStyle.Group, textSuffix: " ");

        Assert.Equal("DENSITY ALTITUDE ONE THOUSAND ONE HUNDRED FT", node.TextAtis);
        Assert.Equal("Density altitude one thousand one hundred feet", node.VoiceAtis);
    }

    [Fact]
    public void Parse_Suffix_IsAppendedToValue()
    {
        var node = Parse(1000, NumberStyle.Default, NumberStyle.Group, " feet MSL");

        Assert.Equal("Density altitude one thousand feet MSL feet", node.VoiceAtis);
    }

    [Fact]
    public void Parse_Negative_PrefixesMinus()
    {
        var node = Parse(-500, NumberStyle.Default, NumberStyle.Group);

        Assert.Equal("DENSITY ALTITUDE -500FT", node.TextAtis);
        Assert.Equal("Density altitude minus five hundred feet", node.VoiceAtis);
    }
}
