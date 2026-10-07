using Vatsim.Vatis.Atis.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis.Nodes;

public class TemperatureNodeTests
{
    private const string Metar = "EPWA 011650Z 25009KT 9999 NSC M05/M06 Q1015";
    private const string PositiveMetar = "EPWA 011650Z 25009KT 9999 NSC 05/02 Q1015";

    private static string ParseTemp(string template, string metar = Metar)
    {
        var node = new TemperatureNode { Station = new AtisStation { Identifier = "EPWA", Name = "EPWA" } };
        return node.ParseTextVariables(new MetarDecoder().ParseNotStrict(metar).AirTemperature!, template);
    }

    private static string ParseDew(string template, string metar = Metar)
    {
        var node = new DewpointNode { Station = new AtisStation { Identifier = "EPWA", Name = "EPWA" } };
        return node.ParseTextVariables(new MetarDecoder().ParseNotStrict(metar).DewPointTemperature!, template);
    }

    [Theory]
    [InlineData("{temp}", "M05")]
    [InlineData("{temp:##}", "M05")]
    [InlineData("{temp:#}", "M5")]
    [InlineData("{temp:M}", "05")]
    [InlineData("{temp:#M}", "5")]
    [InlineData("{temp:-}", "-05")]
    [InlineData("{temp:#-}", "-5")]
    [InlineData("{temp:##'MS'}", "MS05")]
    [InlineData("{temp:#'MS'}", "MS5")]
    [InlineData("T {temp:'MS'}", "T MS05")]
    public void Temperature_Negative(string template, string expected)
    {
        Assert.Equal(expected, ParseTemp(template));
    }

    [Theory]
    [InlineData("{temp}", "05")]
    [InlineData("{temp:-}", "05")]
    [InlineData("{temp:##'MS'}", "05")]
    public void Temperature_Positive(string template, string expected)
    {
        Assert.Equal(expected, ParseTemp(template, PositiveMetar));
    }

    [Theory]
    [InlineData("{dewpoint:-}", "-06")]
    [InlineData("{dewpoint:#'MS'}", "MS6")]
    [InlineData("{dewpoint:M}", "06")]
    public void Dewpoint_Negative(string template, string expected)
    {
        Assert.Equal(expected, ParseDew(template));
    }
}
