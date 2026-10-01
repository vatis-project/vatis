using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Xunit;

namespace Vatsim.Vatis.Tests.Weather;

public class MetarDecoderTests
{
    private readonly MetarDecoder _decoder = new();

    [Fact]
    public void Parse_UnavailableRvr_DecodesRemainingGroups()
    {
        var metar = _decoder.ParseNotStrict(
            "ESOW 021850Z AUTO 01017KT 1300 R01/P2000N R19///// // VV016 M04/M05 Q0983");

        Assert.NotNull(metar.AirTemperature);
        Assert.NotNull(metar.DewPointTemperature);
        Assert.NotNull(metar.Pressure);
    }

    [Fact]
    public void Parse_UnavailableGroupAfterVisibility_DecodesRemainingGroups()
    {
        var metar = _decoder.ParseNotStrict(
            "ELLX 241050Z 09004KT 050V120 0150 0050// R24/0150N FG VV001 00/00 Q1001 NOSIG");

        Assert.Single(metar.RunwaysVisualRange!);
        Assert.NotNull(metar.AirTemperature);
        Assert.NotNull(metar.Pressure);
        Assert.NotEmpty(metar.PresentWeather);
    }

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
    public void Parse_TempoWithMultiplePresentWeather_DecodesTrend()
    {
        var metar = _decoder.ParseNotStrict(
            "LOWW 261750Z 11010KT 9999 FEW007 BKN026 13/12 Q1024 TEMPO 4000 SHRA BR SCT004 BKN008");

        Assert.NotNull(metar.TrendForecast);
        Assert.Equal("SHRA BR ", metar.TrendForecast.WeatherCodes);
        Assert.Equal("SCT004 BKN008 ", metar.TrendForecast.Clouds);
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

    [Fact]
    public void Parse_VisibilityInKilometers_DecodesAllFollowingGroups()
    {
        var metar = _decoder.ParseNotStrict(
            "NZNV 300700Z 24023G35KT 18KM -RA VCTS FEW019 BKN055 OVC120 06/04 Q0994 RMK AUTO VATSIM USE ONLY");

        Assert.Equal(18000, metar.Visibility?.PrevailingVisibility?.ActualValue);
        Assert.Equal(Value.Unit.Meter, metar.Visibility?.PrevailingVisibility?.ActualUnit);
        Assert.NotEmpty(metar.PresentWeather);
        Assert.NotEmpty(metar.Clouds);
        Assert.NotNull(metar.AirTemperature);
        Assert.NotNull(metar.Pressure);
    }

    [Fact]
    public void Parse_VisibilityWithDirectionalMinimum_DecodesMinimumAndFollowingGroups()
    {
        var metar = _decoder.ParseNotStrict(
            "VVNB 230700Z 30007KT 260V320 7000 4000E RA FEW004 SCT046 OVC077 27/26 Q1001");

        Assert.Equal(7000, metar.Visibility?.PrevailingVisibility?.ActualValue);
        Assert.Equal(4000, metar.Visibility?.MinimumVisibility?.ActualValue);
        Assert.Equal("E", metar.Visibility?.MinimumVisibilityDirection);
        Assert.NotEmpty(metar.Clouds);
    }
}
