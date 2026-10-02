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
}
