using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Xunit;

namespace Vatsim.Vatis.Tests.Weather;

public class MetarDecoderTests
{
    private readonly MetarDecoder _decoder = new();

    [Fact]
    public void Parse_NosigWithoutRemarks_DecodesTrend()
    {
        var metar = _decoder.ParseNotStrict(
            "METAR UNNT 290800Z 11003MPS 040V170 CAVOK 31/17 Q1002 R07/CLRD// R34/////// NOSIG");

        Assert.NotNull(metar.TrendForecast);
        Assert.Equal(TrendForecastType.NoSignificantChanges, metar.TrendForecast.ChangeIndicator);
    }

    [Fact]
    public void Parse_NosigWithRemarks_DecodesTrend()
    {
        var metar = _decoder.ParseNotStrict(
            "METAR UNNT 290800Z 11003MPS 040V170 CAVOK 31/17 Q1002 R07/CLRD// R34/////// NOSIG RMK QFE742/0989");

        Assert.NotNull(metar.TrendForecast);
        Assert.Equal(TrendForecastType.NoSignificantChanges, metar.TrendForecast.ChangeIndicator);
    }

    [Fact]
    public void Parse_TempoWithRemarks_DecodesTrend()
    {
        var metar = _decoder.ParseNotStrict(
            "METAR UNNT 241813Z 20003MPS 4900 BR OVC002 05/05 Q0998 R25/250250 TEMPO 0300 FG VV001 RMK OBST OBSC QFE743");

        Assert.NotNull(metar.TrendForecast);
        Assert.Equal(TrendForecastType.Temporary, metar.TrendForecast.ChangeIndicator);
    }

    [Fact]
    public void Parse_WithRemarks_PreservesRawMetar()
    {
        var metar = _decoder.ParseNotStrict("METAR UNNT 290800Z 11003MPS CAVOK 31/17 Q1002 NOSIG RMK QFE742/0989");

        Assert.Contains("RMK QFE742/0989", metar.RawMetar);
    }

    [Fact]
    public void Parse_WithoutTrend_LeavesTrendNull()
    {
        var metar = _decoder.ParseNotStrict("METAR UNNT 290800Z 11003MPS CAVOK 31/17 Q1002 RMK QFE742/0989");

        Assert.Null(metar.TrendForecast);
    }
}
