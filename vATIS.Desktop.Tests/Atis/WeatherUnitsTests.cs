// <copyright file="WeatherUnitsTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Weather.Decoder;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

/// <summary>
/// Checks that weather conditions work with the units used by both ICAO and FAA (US) METARs.
/// </summary>
public class WeatherUnitsTests
{
    private static readonly AtisFormat s_format = new();

    private static bool Met(string metar, string expression)
    {
        var decoded = new MetarDecoder().ParseNotStrict(metar);
        Assert.True(WeatherExpression.TryParse(expression, out var parsed, out var error), $"'{expression}': {error}");
        return parsed!.Evaluate(decoded, s_format);
    }

    [Theory]
    [InlineData("KLAX 241553Z 25008KT 1/2SM FG OVC002 14/13 A2992", "visibility = 0.5sm", true)]
    [InlineData("KLAX 241553Z 25008KT 1/2SM FG OVC002 14/13 A2992", "visibility = 1/2sm", true)]
    [InlineData("KLAX 241553Z 25008KT 1/2SM FG OVC002 14/13 A2992", "visibility < 1sm", true)]
    [InlineData("KLAX 241553Z 25008KT 1/2SM FG OVC002 14/13 A2992", "visibility < 1/2sm", false)]
    [InlineData("KLAX 241553Z 25008KT 1/2SM FG OVC002 14/13 A2992", "visibility <= 1/2sm", true)]
    [InlineData("KLAX 241553Z 25008KT 1/2SM FG OVC002 14/13 A2992", "visibility < 900m", true)]
    [InlineData("KLAX 241553Z 25008KT 1/2SM FG OVC002 14/13 A2992", "visibility < 800m", false)]
    [InlineData("KJFK 241551Z 18012KT 1 1/2SM BR BKN008 18/17 A3005", "visibility = 1.5sm", true)]
    [InlineData("KJFK 241551Z 18012KT 1 1/2SM BR BKN008 18/17 A3005", "visibility = 1 1/2sm", true)]
    [InlineData("KJFK 241551Z 18012KT 1 1/2SM BR BKN008 18/17 A3005", "visibility < 1 1/2 sm", false)]
    [InlineData("KJFK 241551Z 18012KT 1 1/2SM BR BKN008 18/17 A3005", "visibility <= 2sm", true)]
    [InlineData("KSFO 241556Z 28010KT 10SM FEW015 15/10 A3001", "visibility >= 10sm", true)]
    [InlineData("KSFO 241556Z 28010KT 10SM FEW015 15/10 A3001", "visibility > 10sm", false)]
    [InlineData("KSFO 241556Z 28010KT 10SM FEW015 15/10 A3001", "visibility >= 16km", true)]
    [InlineData("CYYZ 241600Z 27010KT 15SM FEW040 12/05 A2995", "visibility >= 10sm", true)]
    [InlineData("EGLL 241550Z 24010KT 9999 FEW040 12/05 Q1015", "visibility >= 6sm", true)]
    [InlineData("EGLL 241550Z 24010KT 4000 BR OVC004 12/11 Q1015", "visibility < 3sm", true)]
    [InlineData("EGLL 241550Z 24010KT 4000 BR OVC004 12/11 Q1015", "visibility < 2.5sm", true)]
    [InlineData("EGLL 241550Z 24010KT 4000 BR OVC004 12/11 Q1015", "visibility < 2.4sm", false)]
    [InlineData("EGLL 241550Z 24010KT 0800 FG OVC001 12/12 Q1015", "visibility < 0.4sm", false)]
    [InlineData("EGLL 241550Z 24010KT 0800 FG OVC001 12/12 Q1015", "visibility < 1sm", true)]
    [InlineData("EGLL 241550Z 24010KT CAVOK 12/05 Q1015", "visibility >= 6sm", true)]
    public void Visibility_StatuteMiles_FromIcaoAndFaaMetars(string metar, string expression, bool expected)
    {
        Assert.Equal(expected, Met(metar, expression));
    }

    [Theory]
    [InlineData("KLAX 241553Z 25008KT 1/4SM R25L/2400FT FG OVC002 14/13 A2992", "rvr < 2600ft", true)]
    [InlineData("KLAX 241553Z 25008KT 1/4SM R25L/2400FT FG OVC002 14/13 A2992", "rvr < 2400ft", false)]
    [InlineData("KLAX 241553Z 25008KT 1/4SM R25L/2400FT FG OVC002 14/13 A2992", "rvr < 800m", true)]
    [InlineData("KLAX 241553Z 25008KT 1/4SM R25L/2400FT FG OVC002 14/13 A2992", "rvr < 0.7km", false)]
    [InlineData("EPWA 191200Z 24005KT 0400 R33/0500N FG OVC001 10/09 Q1013", "rvr < 2000ft", true)]
    [InlineData("EPWA 191200Z 24005KT 0400 R33/0500N FG OVC001 10/09 Q1013", "rvr < 550m", true)]
    [InlineData("EPWA 191200Z 24005KT 0400 R33/0500N FG OVC001 10/09 Q1013", "rvr < 500m", false)]
    public void Rvr_InFeetAndMeters(string metar, string expression, bool expected)
    {
        Assert.Equal(expected, Met(metar, expression));
    }

    [Theory]
    [InlineData("KSFO 241556Z 28010KT 10SM OVC008 15/10 A3001", "ceiling < 1000ft", true)]
    [InlineData("KSFO 241556Z 28010KT 10SM OVC008 15/10 A3001", "ceiling < 300m", true)]
    [InlineData("KSFO 241556Z 28010KT 10SM OVC008 15/10 A3001", "ceiling < 200m", false)]
    [InlineData("KSFO 241556Z 28010KT 10SM OVC008 15/10 A3001", "cloudbase = 800ft", true)]
    public void Ceiling_InHundredsOfFeet(string metar, string expression, bool expected)
    {
        Assert.Equal(expected, Met(metar, expression));
    }

    [Theory]
    [InlineData("KLAX 241553Z 25008KT 10SM FEW015 14/13 A2992", "qnh = 29.92inhg", true)]
    [InlineData("KLAX 241553Z 25008KT 10SM FEW015 14/13 A2992", "qnh >= 1013hpa", true)]
    [InlineData("KLAX 241553Z 25008KT 10SM FEW015 14/13 A2992", "qnh < 30inhg", true)]
    [InlineData("EGLL 241550Z 24010KT 9999 FEW040 12/05 Q1015", "qnh = 1015hpa", true)]
    [InlineData("EGLL 241550Z 24010KT 9999 FEW040 12/05 Q1015", "qnh > 29.9inhg", true)]
    [InlineData("EGLL 241550Z 24010KT 9999 FEW040 12/05 Q1015", "qnh < 29.9inhg", false)]
    public void Pressure_InInchesAndHectopascals(string metar, string expression, bool expected)
    {
        Assert.Equal(expected, Met(metar, expression));
    }

    [Theory]
    [InlineData("KBOS 241554Z 30008KT 10SM CLR M05/M08 A3010", "temperature < 0c", true)]
    [InlineData("KBOS 241554Z 30008KT 10SM CLR M05/M08 A3010", "temperature = -5c", true)]
    [InlineData("KBOS 241554Z 30008KT 10SM CLR M05/M08 A3010", "temperature = 23f", true)]
    [InlineData("KBOS 241554Z 30008KT 10SM CLR M05/M08 A3010", "dewpoint = -8c", true)]
    [InlineData("KBOS 241554Z 30008KT 10SM CLR M05/M08 A3010", "dewpoint < 20f", true)]
    public void Temperature_InCelsiusAndFahrenheit(string metar, string expression, bool expected)
    {
        Assert.Equal(expected, Met(metar, expression));
    }

    [Theory]
    [InlineData("KDEN 241553Z 27015G25KT 10SM CLR 10/M02 A3000", "windspeed = 15kt", true)]
    [InlineData("KDEN 241553Z 27015G25KT 10SM CLR 10/M02 A3000", "gust >= 25kt", true)]
    [InlineData("KDEN 241553Z 27015G25KT 10SM CLR 10/M02 A3000", "winddir = 270deg", true)]
    [InlineData("KDEN 241553Z 27015G25KT 10SM CLR 10/M02 A3000", "windspeed < 7mps", false)]
    [InlineData("KDEN 241553Z 27015G25KT 10SM CLR 10/M02 A3000", "windspeed < 8mps", true)]
    [InlineData("KDEN 241553Z 27015G25KT 10SM CLR 10/M02 A3000", "windspeed < 9mps", true)]
    [InlineData("ULLI 241530Z 24005MPS 9999 BKN033 10/05 Q1015", "windspeed = 5mps", true)]
    [InlineData("ULLI 241530Z 24005MPS 9999 BKN033 10/05 Q1015", "windspeed < 10kt", true)]
    [InlineData("VHHH 241530Z 09010KMH 9999 FEW020 25/20 Q1012", "windspeed = 10kmh", true)]
    [InlineData("KDEN 241553Z 00000KT 10SM CLR 10/M02 A3000", "windspeed = 0kt", true)]
    [InlineData("KDEN 241553Z VRB03KT 10SM CLR 10/M02 A3000", "winddir = 0deg", false)]
    public void Wind_InKnotsKilometersPerHourAndMetersPerSecond(string metar, string expression, bool expected)
    {
        Assert.Equal(expected, Met(metar, expression));
    }

    [Theory]
    [InlineData("visibility < 1/0sm", "divided by zero")]
    [InlineData("rvr < 1sm", "not a unit for rvr")]
    [InlineData("ceiling < 1sm", "not a unit for ceiling")]
    public void InvalidUnitsAndFractions_ReportAClearError(string expression, string expectedMessage)
    {
        Assert.False(WeatherExpression.TryParse(expression, out _, out var error));
        Assert.Contains(expectedMessage, error);
    }

    [Theory]
    [InlineData("KLAX 241553Z 25008KT M1/4SM FG VV002 14/13 A2992", "visibility <= 1/4sm", true)]
    [InlineData("KLAX 241553Z 25008KT M1/4SM FG VV002 14/13 A2992", "visibility < 1sm", true)]
    [InlineData("KLAX 241553Z 25008KT 1/4SM R25L/P6000FT FG OVC002 14/13 A2992", "rvr >= 6000ft", true)]
    [InlineData("KLAX 241553Z 25008KT 1/4SM R25L/M0600FT FG OVC002 14/13 A2992", "rvr <= 600ft", true)]
    [InlineData("KLAX 241553Z 25008KT 1/4SM R25L/1800V3000FT FG OVC002 14/13 A2992", "rvr < 2000ft", true)]
    [InlineData("KLAX 241553Z 25008KT 1/4SM FG VV002 14/13 A2992", "ceiling <= 200ft", true)]
    [InlineData("KLAX 241553Z 25008KT 1/4SM FG VV002 14/13 A2992", "cloudbase <= 200ft", true)]
    [InlineData("KLAX 241553Z 25008KT 3SM BR OVC004 14/13 A2992", "ceiling < 500ft", true)]
    [InlineData("KLAX 241553Z 25008KT 3SM BR SCT004 BKN010 14/13 A2992", "ceiling < 500ft", false)]
    public void FaaSpecificForms(string metar, string expression, bool expected)
    {
        Assert.Equal(expected, Met(metar, expression));
    }
}
