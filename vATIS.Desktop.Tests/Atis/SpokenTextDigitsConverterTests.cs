using Vatsim.Vatis.Atis.Extensions;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class SpokenTextDigitsConverterTests
{
    [Fact]
    public void Convert_TurnsSampleAtisIntoDigits()
    {
        const string input =
            "FORT LAUDERDALE EXECUTIVE AIRPORT ATIS INFO UNIFORM TWO ZERO FIVE THREE ZULU. WIND ZERO NINER ZERO AT EIGHT. " +
            "VISIBILITY ONE ZERO.. FOUR THOUSAND TWO HUNDRED SCATTERED. TEMPERATURE TWO EIGHT. ALTIMETER TWO NINER NINER FOUR.... " +
            "LNDG AND DEPG RUNWAYS NINER AND ONE THREE. ADVISE ON INITIAL CONTACT, YOU HAVE INFORMATION UNIFORM.";

        const string expected =
            "FORT LAUDERDALE EXECUTIVE AIRPORT ATIS INFO U 2053 ZULU. WIND 090 AT 8. " +
            "VISIBILITY 10.. 4200 SCATTERED. TEMPERATURE 28. ALTIMETER 2994.... " +
            "LNDG AND DEPG RUNWAYS 9 AND 13. ADVISE ON INITIAL CONTACT, YOU HAVE INFORMATION U.";

        Assert.Equal(expected, SpokenTextDigitsConverter.Convert(input));
    }

    [Fact]
    public void Convert_HandlesRunwaySidesFrequenciesAndTaxiways()
    {
        Assert.Equal("RUNWAY 28R", SpokenTextDigitsConverter.Convert("RUNWAY TWO EIGHT RIGHT"));
        Assert.Equal("CONTACT 121.7", SpokenTextDigitsConverter.Convert("CONTACT ONE TWO ONE DECIMAL SEVEN"));
        Assert.Equal("TAXIWAY A", SpokenTextDigitsConverter.Convert("TAXIWAY ALPHA"));
        Assert.Equal("20000 SCATTERED", SpokenTextDigitsConverter.Convert("TWO ZERO THOUSAND SCATTERED"));
        Assert.Equal("12500 BROKEN", SpokenTextDigitsConverter.Convert("ONE TWO THOUSAND FIVE HUNDRED BROKEN"));
        Assert.Equal("SKY CLEAR BELOW one-two thousand", SpokenTextDigitsConverter.Convert("SKY CLEAR BELOW one-two thousand"));
        Assert.Equal("3000 BROKEN", SpokenTextDigitsConverter.Convert("THREE THOUSAND BROKEN"));
    }
}
