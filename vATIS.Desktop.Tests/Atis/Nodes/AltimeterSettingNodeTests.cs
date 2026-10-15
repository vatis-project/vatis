using System.Threading.Tasks;
using System;
using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class AltimeterSettingNodeTests
{
    [Theory]
    [InlineData("EPWA 011650Z 25009KT 9999 NSC 05/02 Q1015", 1015)]
    [InlineData("KJFK 011651Z 25009KT 10SM CLR 05/02 A3000", 1015)]
    public void GetQnhHpa_ReturnsHectopascals(string metar, int expected)
    {
        var pressure = new MetarDecoder().ParseNotStrict(metar).Pressure?.Value;
        Assert.Equal(expected, AltimeterSettingNode.GetQnhHpa(pressure));
    }

    [Fact]
    public void GetQnhHpa_NullPressure_ReturnsNull()
    {
        Assert.Null(AltimeterSettingNode.GetQnhHpa(null));
    }

    [Theory]
    [InlineData(1015, 0, 1015)]
    [InlineData(1015, 273, 1005)]
    [InlineData(1015, 300, 1005)]
    public void CalculateQfe_AppliesLapseRate(int qnh, int elevation, int expected)
    {
        Assert.Equal(expected, AltimeterSettingNode.CalculateQfe(qnh, elevation));
    }

    private static async Task<AltimeterSettingNode> ParseAsync(
        Action<Vatsim.Vatis.Profiles.AtisFormat.Nodes.Altimeter> configure)
    {
        var station = new Vatsim.Vatis.Profiles.Models.AtisStation { Identifier = "EPWA", Name = "EPWA" };
        station.AtisFormat.Altimeter.Template.Text = "{altimeter}|{altimeter|hpa}|{altimeter|inhg}|{altimeter|text}";
        station.AtisFormat.Altimeter.Template.Voice = "{altimeter}|{altimeter|hpa}|{altimeter|text}";
        configure(station.AtisFormat.Altimeter);
        var node = new AltimeterSettingNode { Station = station };
        await node.Parse(new MetarDecoder().ParseNotStrict("EPWA 011650Z 25009KT 9999 NSC 05/02 Q1015"), null!);
        return node;
    }

    [Fact]
    public async Task VariableFormats_Default_ReproducesExistingOutput()
    {
        var node = await ParseAsync(_ => { });
        Assert.Equal("1015|1015|29.97|ONE ZERO ONE FIVE", node.TextAtis);
        Assert.Equal("one zero one five|one zero one five|one zero one five", node.VoiceAtis, ignoreCase: true);
    }

    [Fact]
    public async Task VariableFormats_TextAndVoiceAreIndependent()
    {
        var node = await ParseAsync(a =>
        {
            a.VariableFormats["hpa"].Text.Suffix = " HPA";
            a.VariableFormats["hpa"].Voice.Style = Vatsim.Vatis.Profiles.AtisFormat.Nodes.NumberStyle.Group;
            a.VariableFormats["hpa"].Voice.Suffix = " hectopascals";
        });
        Assert.StartsWith("1015|1015 HPA|", node.TextAtis);
        Assert.Contains("hectopascals", node.VoiceAtis);
        Assert.DoesNotContain("HPA", node.VoiceAtis);
    }

    [Fact]
    public async Task VariableFormats_DigitsStyle_InVoice()
    {
        var node = await ParseAsync(a =>
            a.VariableFormats["altimeter"].Voice.Style = Vatsim.Vatis.Profiles.AtisFormat.Nodes.NumberStyle.Digits);
        Assert.StartsWith("1015|", node.VoiceAtis);
    }
}
