// <copyright file="WeatherRuleEvaluatorTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Linq;
using System.Text.Json;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class WeatherRuleEvaluatorTests
{
    private static readonly AtisFormat s_format = new();

    private static DecodedMetar Metar(string raw) => new MetarDecoder().ParseNotStrict(raw);

    private static bool Met(DecodedMetar? metar, string expression)
    {
        Assert.True(
            WeatherExpression.TryParse(expression, out var parsed, out var error),
            $"'{expression}' should parse: {error}");
        return parsed!.Evaluate(metar, s_format);
    }

    private static string ParseError(string expression)
    {
        Assert.False(WeatherExpression.TryParse(expression, out _, out var error), $"'{expression}' should not parse");
        return error!;
    }

    [Fact]
    public void NoMetar_ComparisonNotMet()
    {
        Assert.False(Met(null, "ceiling < 200ft"));
        Assert.False(Met(null, "NOT windshear"));
    }

    [Theory]
    [InlineData("EPWA 191200Z 24005KT 9999 OVC001 10/09 Q1013", true)]
    [InlineData("EPWA 191200Z 24005KT 9999 BKN001 10/09 Q1013", true)]
    [InlineData("EPWA 191200Z 24005KT 9999 BKN002 10/09 Q1013", false)] // exactly 200 ft is not below 200 ft
    [InlineData("EPWA 191200Z 24005KT 9999 OVC003 10/09 Q1013", false)]
    [InlineData("EPWA 191200Z 24005KT 9999 FEW001 10/09 Q1013", false)] // few is not a ceiling
    [InlineData("EPWA 191200Z 24005KT CAVOK 10/09 Q1013", false)]
    public void Ceiling_Below_Feet(string raw, bool expected)
    {
        Assert.Equal(expected, Met(Metar(raw), "ceiling < 200ft"));
    }

    [Fact]
    public void Ceiling_ConvertsUnits()
    {
        var metar = Metar("EPWA 191200Z 24005KT 9999 OVC001 10/09 Q1013");

        // 100 ft is about 30 m.
        Assert.True(Met(metar, "ceiling < 60m"));
        Assert.False(Met(metar, "ceiling < 25m"));
        Assert.True(Met(metar, "ceiling < 200"), "feet is the default unit for a ceiling");
    }

    [Fact]
    public void Rvr_UsesLowestReported_AndMissingIsNotMet()
    {
        var metar = Metar("EPWA 191200Z 24005KT 0400 R33/0500N R15/0800N FG OVC001 10/09 Q1013");
        Assert.True(Met(metar, "rvr < 550m"));
        Assert.False(Met(metar, "rvr > 550m"));

        var noRvr = Metar("EPWA 191200Z 24005KT 0400 FG OVC001 10/09 Q1013");
        Assert.False(Met(noRvr, "rvr < 550m"));
        Assert.False(Met(noRvr, "rvr > 550m"));
    }

    [Fact]
    public void Visibility_MetersKilometersStatuteMilesAndCavok()
    {
        Assert.True(Met(Metar("EPWA 191200Z 24005KT 0800 BR OVC010 10/09 Q1013"), "visibility < 1000m"));
        Assert.False(Met(Metar("EPWA 191200Z 24005KT CAVOK 10/09 Q1013"), "visibility < 1000m"));

        var us = Metar("KJFK 191200Z 24005KT 1/2SM FG OVC001 10/09 A2992");
        Assert.True(Met(us, "vis < 1km"));
        Assert.False(Met(us, "vis < 500m"));
    }

    [Theory]
    [InlineData("windspeed >= 20kt", true)]
    [InlineData("windspeed > 20kt", false)]
    [InlineData("windspeed > 35kmh", true)]
    [InlineData("windspeed < 11m/s", true)]
    [InlineData("gust > 30kt", true)]
    [InlineData("gust > 35kt", false)]
    [InlineData("winddir = 240", true)]
    [InlineData("winddir < 200deg", false)]
    public void Wind(string expression, bool expected)
    {
        var metar = Metar("EPWA 191200Z 24020G32KT 9999 FEW030 10/09 Q1013");
        Assert.Equal(expected, Met(metar, expression));
    }

    [Fact]
    public void Gust_AndDirection_NotMetWhenNotReported()
    {
        var calm = Metar("EPWA 191200Z VRB03KT 9999 FEW030 10/09 Q1013");
        Assert.False(Met(calm, "gust > 0kt"));
        Assert.False(Met(calm, "winddir > 0"));
    }

    [Theory]
    [InlineData("temperature < 1c", true)]
    [InlineData("temp < 33f", true)]
    [InlineData("temp < 31f", false)]
    [InlineData("dewpoint < -1c", true)]
    [InlineData("qnh < 1000hpa", true)]
    [InlineData("qnh < 29.5inhg", true)]
    [InlineData("qnh > 29.5inhg", false)]
    public void TemperatureDewpointAndPressure(string expression, bool expected)
    {
        var metar = Metar("EPWA 191200Z 24005KT 9999 FEW030 00/M02 Q0995");
        Assert.Equal(expected, Met(metar, expression));
    }

    [Fact]
    public void Pressure_InchesReportedInUsMetar()
    {
        var metar = Metar("KJFK 191200Z 24005KT 10SM FEW030 10/09 A2992");
        Assert.True(Met(metar, "qnh < 30inhg"));
        Assert.True(Met(metar, "qnh > 1010hpa"));
    }

    [Fact]
    public void LowestCloudBase_CountsAnyCoverage()
    {
        var metar = Metar("EPWA 191200Z 24005KT 9999 FEW005 BKN030 10/09 Q1013");
        Assert.True(Met(metar, "cloudbase < 1000ft"));
        Assert.False(Met(metar, "ceiling < 1000ft"));
    }

    [Fact]
    public void PresentWeather_Codes()
    {
        var metar = Metar("EPWA 191200Z 24005KT 3000 +TSRA BKN030CB 10/09 Q1013");
        Assert.True(Met(metar, "weather includes TS"));
        Assert.True(Met(metar, "weather includes \"fzra ra\""));
        Assert.True(Met(metar, "weather includes fzra,ra"));
        Assert.False(Met(metar, "weather includes \"FZRA SN\""));
        Assert.True(Met(metar, "weather excludes SN"));
        Assert.False(Met(metar, "weather excludes RA"));
    }

    [Fact]
    public void TrendWeather_Codes()
    {
        var metar = Metar("EKCH 251050Z 21017KT 9999 SCT014 11/09 Q0986 TEMPO SHRA BKN030CB");
        Assert.True(Met(metar, "trendweather includes SHRA"));
        Assert.True(Met(metar, "trendwx includes RA"));
        Assert.False(Met(metar, "trendweather excludes RA"));
        Assert.False(Met(metar, "trendweather includes TS"));
        Assert.False(Met(metar, "weather includes SHRA"));
        Assert.True(Met(metar, "weather excludes SHRA"));
    }

    [Fact]
    public void Trend_CloudsVisibilityAndWind()
    {
        var metar = Metar("EKCH 251050Z 21017KT 9999 SCT014 11/09 Q0986 TEMPO 4000 SHRA BKN030CB");
        Assert.True(Met(metar, "trendcloudtype includes CB"));
        Assert.False(Met(metar, "cloudtype includes CB"));
        Assert.True(Met(metar, "trendceiling < 1000m"));
        Assert.False(Met(metar, "trendceiling < 500m"));
        Assert.True(Met(metar, "ceiling > 5000ft"));
        Assert.True(Met(metar, "trendcloudbase = 3000ft"));
        Assert.True(Met(metar, "trendvisibility < 5000m"));
        Assert.False(Met(metar, "visibility < 5000m"));

        var wind = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 BECMG 25025G40KT");
        Assert.True(Met(wind, "trendwindspeed >= 25kt"));
        Assert.True(Met(wind, "trendgust > 30kt"));
        Assert.True(Met(wind, "trendwinddir = 250"));
        Assert.True(Met(wind, "windspeed < 15kt"));

        var cavok = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 BECMG CAVOK");
        Assert.True(Met(cavok, "trendcavok"));
        Assert.False(Met(cavok, "cavok"));
    }

    [Fact]
    public void Trend_FullGroup()
    {
        var metar = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 BECMG FM1200 TL1400 25025G40KT 6000 -RA BKN008");
        Assert.True(Met(metar, "trendwindspeed >= 25kt"));
        Assert.True(Met(metar, "trendgust >= 40kt"));
        Assert.True(Met(metar, "trendvisibility = 6000m"));
        Assert.True(Met(metar, "trendweather includes -RA"));
        Assert.True(Met(metar, "trendceiling = 800ft"));
    }

    [Fact]
    public void Trend_NoSignificantWeather()
    {
        var metar = Metar("EKCH 251050Z 21010KT 9999 -RA SCT014 11/09 Q0986 BECMG NSW");
        Assert.True(Met(metar, "trendweather includes NSW"));
    }

    [Fact]
    public void Trend_TwoGroups()
    {
        var metar = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 TEMPO SHRA BECMG 5000 TS");
        Assert.True(Met(metar, "trendweather includes SHRA"));
        Assert.True(Met(metar, "trendweather includes TS"));
        Assert.False(Met(metar, "trendweather excludes TS"));
        Assert.True(Met(metar, "trendweather excludes FG"));
    }

    [Fact]
    public void TempoAndBecmg_OnlyReadTheirOwnGroups()
    {
        var tempo = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 TEMPO RA");
        Assert.True(Met(tempo, "tempoweather includes RA"));
        Assert.False(Met(tempo, "becmgweather includes RA"));
        Assert.True(Met(tempo, "trendweather includes RA"));
        Assert.True(Met(tempo, "becmgweather excludes RA"));
        Assert.False(Met(tempo, "tempoweather excludes RA"));

        var becmg = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 BECMG RA");
        Assert.False(Met(becmg, "tempoweather includes RA"));
        Assert.True(Met(becmg, "becmgweather includes RA"));
    }

    [Fact]
    public void TempoAndBecmg_MixedGroups()
    {
        var metar = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 TEMPO 5000 BECMG RA");
        Assert.False(Met(metar, "tempoweather includes RA"));
        Assert.True(Met(metar, "becmgweather includes RA"));
        Assert.True(Met(metar, "tempovisibility < 6000m"));
        Assert.False(Met(metar, "becmgvisibility < 6000m"));

        var reversed = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 TEMPO RA BECMG NSW");
        Assert.True(Met(reversed, "tempoweather includes RA"));
        Assert.True(Met(reversed, "becmgweather excludes RA"));
    }

    [Fact]
    public void TempoAndBecmg_OtherFields()
    {
        var metar = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 TEMPO 0200 BKN005CB");
        Assert.True(Met(metar, "tempovisibility < 500m"));
        Assert.True(Met(metar, "tempoceiling = 500ft"));
        Assert.True(Met(metar, "tempocloudtype includes CB"));
        Assert.False(Met(metar, "becmgcloudtype includes CB"));
        Assert.Contains("Unknown field", ParseError("tempotemp > 10c"));
        Assert.Contains("Unknown field", ParseError("becmgrvr < 500m"));
    }

    [Fact]
    public void TrendType()
    {
        var tempo = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 TEMPO SHRA");
        Assert.True(Met(tempo, "trendtype includes TEMPO"));
        Assert.False(Met(tempo, "trendtype includes BECMG"));
        Assert.True(Met(tempo, "trendtype includes \"becmg tempo\""));
        Assert.True(Met(tempo, "trendtype excludes BECMG"));
        Assert.False(Met(tempo, "trendtype excludes TEMPO"));

        var both = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 TEMPO SHRA BECMG 5000 TS");
        Assert.True(Met(both, "trendtype includes TEMPO"));
        Assert.True(Met(both, "trendtype includes BECMG"));
        Assert.False(Met(both, "trendtype excludes BECMG"));

        var nosig = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986 NOSIG");
        Assert.True(Met(nosig, "trendtype includes NOSIG"));
        Assert.True(Met(nosig, "trendtype excludes TEMPO"));

        var none = Metar("EKCH 251050Z 21010KT 9999 SCT014 11/09 Q0986");
        Assert.False(Met(none, "trendtype includes TEMPO"));
        Assert.True(Met(none, "trendtype excludes TEMPO"));
    }

    [Fact]
    public void Trend_NotReported()
    {
        var nosig = Metar("EKCH 251050Z 21017KT 9999 SCT014 11/09 Q0986 NOSIG");
        Assert.False(Met(nosig, "trendweather includes RA"));
        Assert.True(Met(nosig, "trendweather excludes RA"));
        Assert.False(Met(nosig, "trendceiling < 1000ft"));

        var none = Metar("EKCH 251050Z 21017KT 9999 SCT014 11/09 Q0986");
        Assert.False(Met(none, "trendweather includes RA"));
        Assert.True(Met(none, "NOT trendweather includes RA"));
    }

    [Fact]
    public void Trend_UnsupportedField()
    {
        Assert.Contains("Unknown field", ParseError("trendtemp > 10c"));
    }

    [Fact]
    public void RecentWeather_Codes()
    {
        var metar = Metar("EKCH 251050Z 21017KT 9999 SCT014 11/09 Q0986 RERA");
        Assert.True(Met(metar, "recentweather includes RERA"));
        Assert.True(Met(metar, "recentweather includes RA"));
        Assert.False(Met(metar, "recentweather includes RESN"));
        Assert.False(Met(metar, "recentweather excludes RERA"));
        Assert.False(Met(metar, "weather includes RA"));
        Assert.False(Met(Metar("EKCH 251050Z 21017KT 9999 SCT014 11/09 Q0986"), "recentweather includes RA"));
    }

    [Fact]
    public void CloudType_Cb()
    {
        var metar = Metar("EPWA 191200Z 24005KT 9999 BKN030CB 10/09 Q1013");
        Assert.True(Met(metar, "cloudtype includes CB"));
        Assert.False(Met(metar, "cloudtype includes TCU"));
    }

    [Fact]
    public void Flags()
    {
        var cavok = Metar("EPWA 191200Z 24005KT CAVOK 10/09 Q1013");
        Assert.True(Met(cavok, "cavok"));
        Assert.False(Met(cavok, "windshear"));
        Assert.True(Met(cavok, "NOT windshear"));

        var shear = Metar("EPWA 191200Z 24005KT 9999 FEW030 10/09 Q1013 WS ALL RWY");
        Assert.True(Met(shear, "windshear"));
    }

    [Theory]
    [InlineData("EPWA 191200Z 24005KT 0050 R33/0400N FG OVC001 10/09 Q1013", true)] // all three
    [InlineData("EPWA 191200Z 24005KT 0800 R33/0400N FG OVC001 10/09 Q1013", true)] // ceiling and RVR
    [InlineData("EPWA 191200Z 24005KT 0800 R33/0400N FG OVC005 10/09 Q1013", false)] // RVR only
    [InlineData("EPWA 191200Z 24005KT 0050 FG OVC005 10/09 Q1013", true)] // visibility only
    [InlineData("EPWA 191200Z 24005KT 0800 FG OVC001 10/09 Q1013", false)] // ceiling only
    public void AndBindsTighterThanOr(string raw, bool expected)
    {
        Assert.Equal(expected, Met(Metar(raw), "ceiling < 200ft AND rvr < 550m OR visibility < 100m"));
    }

    [Theory]
    [InlineData("EPWA 191200Z 24005KT 0800 R33/0400N FG OVC001 10/09 Q1013", true)] // vis, ceiling and RVR
    [InlineData("EPWA 191200Z 24005KT 0800 FG OVC001 10/09 Q1013", true)] // vis and ceiling
    [InlineData("EPWA 191200Z 24005KT 0800 R33/0400N FG OVC010 10/09 Q1013", true)] // vis and RVR
    [InlineData("EPWA 191200Z 24005KT 0800 FG OVC010 10/09 Q1013", false)] // only vis
    [InlineData("EPWA 191200Z 24005KT 2000 R33/0400N OVC001 10/09 Q1013", false)] // vis not met
    public void ParenthesesGroupAnOr(string raw, bool expected)
    {
        Assert.Equal(expected, Met(Metar(raw), "visibility < 1000m AND (ceiling < 200ft OR rvr < 550m)"));
    }

    [Fact]
    public void Parentheses_ChangeTheResult()
    {
        // A met, B and C not met.
        var metar = Metar("EPWA 191200Z 24005KT 0800 FG OVC010 10/09 Q1013");
        Assert.True(Met(metar, "vis < 1000m OR ceiling < 200ft AND rvr < 550m"));
        Assert.False(Met(metar, "(vis < 1000m OR ceiling < 200ft) AND rvr < 550m"));
    }

    [Fact]
    public void Parentheses_Nested()
    {
        var metar = Metar("EPWA 191200Z 24005KT 2000 OVC010 10/09 Q1013");
        const string expression = "((vis < 1000m OR ceiling < 200ft) AND rvr < 550m) OR vis > 1500m";
        Assert.True(Met(metar, expression));
        Assert.False(Met(metar, "((vis < 1000m OR ceiling < 200ft) AND rvr < 550m)"));
    }

    [Fact]
    public void Not_BindsToTheFollowingConditionOrGroup()
    {
        var metar = Metar("EPWA 191200Z 24005KT 0800 FG OVC001 10/09 Q1013");
        Assert.False(Met(metar, "NOT vis < 1000m"));
        Assert.True(Met(metar, "NOT (vis > 1000m OR cloudtype includes CB)"));
        Assert.True(Met(metar, "NOT NOT vis < 1000m"));
        Assert.True(Met(metar, "vis < 1000m AND NOT windshear"));
    }

    [Fact]
    public void SyntaxIsCaseInsensitive_AndWhitespaceFlexible()
    {
        var metar = Metar("EPWA 191200Z 24005KT 0800 FG OVC001 10/09 Q1013");
        Assert.True(Met(metar, "CEILING<200FT and\n  (RVR < 550M or Vis<=1000 M)"));
        Assert.True(Met(metar, "ceiling < 200 ft"));
        Assert.True(Met(metar, "visibility<=800m"));
        Assert.True(Met(metar, "visibility==800m"));
    }

    [Theory]
    [InlineData("", "Enter a condition")]
    [InlineData("   ", "Enter a condition")]
    [InlineData("height < 200ft", "Unknown field 'height'")]
    [InlineData("ceiling 200ft", "Expected a comparison")]
    [InlineData("ceiling <", "Expected a number")]
    [InlineData("ceiling < abc", "Expected a number")]
    [InlineData("ceiling < 200kt", "'kt' is not a unit for ceiling")]
    [InlineData("ceiling < 200ft AND", "ends too soon")]
    [InlineData("ceiling < 200ft AND OR rvr < 5m", "Expected a field name")]
    [InlineData("(ceiling < 200ft", "Missing a closing ')'")]
    [InlineData("ceiling < 200ft)", "without a matching '('")]
    [InlineData("ceiling < 200ft rvr < 5m", "Expected AND or OR")]
    [InlineData("weather TS", "Expected INCLUDES or EXCLUDES")]
    [InlineData("weather includes", "Expected the codes")]
    [InlineData("weather includes \"TS", "Missing closing quote")]
    [InlineData("windshear < 5", "either reported or not")]
    [InlineData("()", "Expected a field name")]
    public void InvalidExpressions_ReportAClearError(string expression, string expectedFragment)
    {
        Assert.Contains(expectedFragment, ParseError(expression));
    }

    [Theory]
    [InlineData("ceiling < 200ft AND (rvr < 550m OR visibility < 100m)")]
    [InlineData("ceiling < 200ft AND rvr < 550m OR visibility < 100m")]
    [InlineData("weather INCLUDES TS,FZRA")]
    [InlineData("weather INCLUDES \"TS FZRA\"")]
    [InlineData("NOT cavok")]
    [InlineData("NOT (windshear OR cloudtype INCLUDES CB)")]
    [InlineData("ceiling < 200ft OR rvr < 550m")]
    [InlineData("weather INCLUDES TS OR cloudtype INCLUDES CB")]
    [InlineData("visibility >= 5km AND NOT weather INCLUDES \"TS RA\"")]
    [InlineData("gust >= 30kt")]
    [InlineData("depth > 3mm")]
    [InlineData("temperature <= 3c AND dewpoint >= -3c")]
    [InlineData("((vis < 1000m OR ceiling < 200ft) AND rvr < 550m) OR wx EXCLUDES FG")]
    public void ExamplesFromTheDocumentationParse(string expression)
    {
        Assert.True(WeatherExpression.TryParse(expression, out _, out var error), error);
    }

    [Fact]
    public void Errors_IncludeAPosition()
    {
        Assert.EndsWith("(position 21)", ParseError("ceiling < 200ft AND foo < 1"));
    }

    [Fact]
    public void Filter_AppliesWeatherAndOnlineConditions()
    {
        var metar = Metar("EPWA 191200Z 24005KT 0400 R33/0500N FG OVC001 10/09 Q1013");
        var lvp = new StaticDefinition("LVP IN OPERATION", 1)
        {
            WeatherCondition = "ceiling < 200ft OR rvr < 550m",
        };

        Assert.Single(StaticDefinitionFilter.GetActive([lvp], null, metar, s_format));
        Assert.Empty(StaticDefinitionFilter.GetActive([lvp], null, null, s_format));
        Assert.Empty(StaticDefinitionFilter.GetActive([lvp], null, metar, null));

        lvp.OnlineCondition = "EPWA_APP";
        Assert.Empty(StaticDefinitionFilter.GetActive([lvp], ["EPWA_TWR"], metar, s_format));
        Assert.Single(StaticDefinitionFilter.GetActive([lvp], ["EPWA_APP"], metar, s_format));
        Assert.True(StaticDefinitionFilter.HasWeatherConditions([lvp]));
        Assert.False(StaticDefinitionFilter.HasWeatherConditions([new StaticDefinition("A", 1)]));
        Assert.False(StaticDefinitionFilter.HasWeatherConditions(
            [new StaticDefinition("A", 1, enabled: false) { WeatherCondition = "cavok" }]));
    }

    [Fact]
    public void Filter_InvalidConditionIsNeverMet()
    {
        var metar = Metar("EPWA 191200Z 24005KT 0400 FG OVC001 10/09 Q1013");
        var bad = new StaticDefinition("X", 1) { WeatherCondition = "ceiling << 200ft" };
        Assert.Empty(StaticDefinitionFilter.GetActive([bad], null, metar, s_format));
    }

    [Fact]
    public void Definition_RoundTripsThroughJson_AndClones()
    {
        var def = new StaticDefinition("LVP", 1) { WeatherCondition = "ceiling < 200ft AND (rvr < 550m OR vis < 100m)" };
        var station = new AtisStation { Identifier = "EPWA", Name = "W", AirportConditionDefinitions = [def] };

        var json = JsonSerializer.Serialize(station, SourceGenerationContext.NewDefault.AtisStation);
        var loaded = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.AtisStation)!;
        Assert.Equal(def.WeatherCondition, loaded.AirportConditionDefinitions.Single().WeatherCondition);
        Assert.Equal(def.WeatherCondition, def.Clone().WeatherCondition);

        var plain = JsonSerializer.Serialize(
            new AtisStation { Identifier = "EPWA", Name = "W", NotamDefinitions = [new StaticDefinition("A", 1)] },
            SourceGenerationContext.NewDefault.AtisStation);
        Assert.DoesNotContain("weatherCondition", plain);
    }

    [Fact]
    public void FieldInfo_EveryFieldHasUnitsOnlyWhenNumeric()
    {
        foreach (var field in WeatherFieldInfo.GetFields())
        {
            Assert.NotEmpty(WeatherFieldInfo.GetOperators(field));
            Assert.Equal(
                WeatherFieldInfo.GetKind(field) == WeatherFieldKind.Numeric,
                WeatherFieldInfo.GetUnits(field).Count > 0);
        }
    }
}
