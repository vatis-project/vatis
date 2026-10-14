// <copyright file="TaxiwayRunwayFormatterTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class TaxiwayRunwayFormatterTests
{
    private static string Normalize(string s) => Regex.Replace(s, @"\s+", " ").Replace(" ,", ",").Replace(" .", ".").Trim().ToUpperInvariant();

    [Theory]
    [InlineData("TWY B CLSD BTN TWY, C7 AND TWY C10 , , TWY B6 CLSD",
        "TWY BRAVO CLSD BTN TWY, CHARLIE SEVEN AND TWY CHARLIE TEN,, TWY BRAVO SIX CLSD")]
    [InlineData("TWY F CLSD BTN TWY B AND, TWY F2", "TWY FOXTROT CLSD BTN TWY BRAVO AND, TWY FOXTROT TWO")]
    [InlineData("TWY L CLSD BTN RY 28L AND, TWY W",
        "TWY LIMA CLSD BTN RUNWAY TWO EIGHT LEFT AND, TWY WHISKEY")]
    [InlineData("TWYS B AND C CLSD", "TWYS BRAVO AND CHARLIE CLSD")]
    [InlineData("DEPG RWYS 1L, 1R.", "DEPG RUNWAYS ONE LEFT, ONE RIGHT.")]
    [InlineData("INSTR DEPARTURES IN PROG RWYS 24 AND 25.",
        "INSTR DEPARTURES IN PROG RUNWAYS TWO FOUR AND TWO FIVE.")]
    [InlineData("PAPI OTS 25R.", "PAPI OTS TWO FIVE RIGHT.")]
    [InlineData("EXPECT DELAYS FOR RWY 28 DEPARTURES", "EXPECT DELAYS FOR RUNWAY TWO EIGHT DEPARTURES")]
    [InlineData("TWYS G1, G2, G3 CLSD", "TWYS GOLF ONE, GOLF TWO, GOLF THREE CLSD")]
    [InlineData("TWY DS AND TWY Z", "TWY DELTA SIERRA AND TWY ZULU")]
    [InlineData("RWYS 4, 22 CLSD", "RUNWAYS FOUR, TWO TWO CLSD")]
    [InlineData("RY 6L, 24R CLSD", "RUNWAY SIX LEFT, TWO FOUR RIGHT CLSD")]
    [InlineData("RWY 1C/19C CLSD", "RUNWAY ONE CENTER, ONE NINER CENTER CLSD")]
    [InlineData("LNDG RWYS 26L AND 1L.", "LNDG RUNWAYS TWO SIX LEFT AND ONE LEFT.")]
    [InlineData("TWY IS CLSD", "TWY IS CLSD")]
    [InlineData("TWY F BTN D AND E 5 CLSD", "TWY FOXTROT BTN DELTA AND ECHO FIVE CLSD")]
    [InlineData("TAXIWAY C, BETWEEN C3, AND S CLOSED", "TAXIWAY CHARLIE, BETWEEN CHARLIE THREE, AND SIERRA CLOSED")]
    [InlineData("TWY A CLOSED BETWEEN D AND M.", "TWY ALPHA CLOSED BETWEEN DELTA AND MIKE.")]
    [InlineData("TWY G CLSD BTN, R AND MID FIELD CARGO", "TWY GOLF CLSD BTN, ROMEO AND MID FIELD CARGO")]
    [InlineData("N BETWEEN RUNWAY 5 AND M", "NOVEMBER BETWEEN RUNWAY FIVE AND MIKE")]
    [InlineData("TWY D, F, K CLSD", "TWY DELTA, FOXTROT, KILO CLSD")]
    [InlineData("TWY U1, TWY U 2, TWY V CLSD", "TWY UNIFORM ONE, TWY UNIFORM TWO, TWY VICTOR CLSD")]
    [InlineData("RY 10, 28 CLSD", "RUNWAY ONE ZERO, TWO EIGHT CLSD")]
    [InlineData("RWY 28 AND 5 MILES", "RUNWAY TWO EIGHT AND 5 MILES")]
    [InlineData("TWY C, DO NOT ENTER", "TWY CHARLIE, DO NOT ENTER")]
    [InlineData("B11 B10 CLSD BTWN, 36R AND, A E1 CLSD", "BRAVO ELEVEN BRAVO TEN CLSD BTWN, THREE SIX RIGHT AND, ALPHA ECHO ONE CLSD")]
    [InlineData("E1 CLSD BTWN, B9 AND, B11 B10 CLSD", "ECHO ONE CLSD BTWN, BRAVO NINE AND, BRAVO ELEVEN BRAVO TEN CLSD")]
    [InlineData("TWYS CLSD, B2, B3, B4, B6, B7, C4", "TWYS CLSD, BRAVO TWO, BRAVO THREE, BRAVO FOUR, BRAVO SIX, BRAVO SEVEN, CHARLIE FOUR")]
    [InlineData("TWY B7 B6 CLSD", "TWY BRAVO SEVEN BRAVO SIX CLSD")]
    [InlineData("RWY 15/33 CLSD", "RUNWAY ONE FIVE, THREE THREE CLSD")]
    [InlineData("RWY 4L/22R CLSD", "RUNWAY FOUR LEFT, TWO TWO RIGHT CLSD")]
    [InlineData("RUNWAY 10/28 NOT AVAILABLE", "RUNWAY ONE ZERO, TWO EIGHT NOT AVAILABLE")]
    [InlineData("TWY NE CLSD", "TWY NOVEMBER ECHO CLSD")]
    [InlineData("TWYS NW AND SE CLSD", "TWYS NOVEMBER WHISKEY AND SIERRA ECHO CLSD")]
    [InlineData("BTN 34 AND NW RAMP", "BTN 34 AND NW RAMP")]
    public void Format_ConvertsIdentifiers(string input, string expected)
    {
        Assert.Equal(expected, Normalize(TaxiwayRunwayFormatter.Format(input, leadingZero: false)));
    }

    [Fact]
    public void Format_DoesNotTouchCaretRunways()
    {
        Assert.Equal("^25L", TaxiwayRunwayFormatter.Format("^25L", leadingZero: false));
    }
}
