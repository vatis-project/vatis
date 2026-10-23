using System.Collections.Generic;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class PatternMatcherTests
{
    private static List<PatternMeta> Regex(string pattern, string text, string voice) =>
        [new PatternMeta { Pattern = pattern, Text = text, Voice = voice }];

    [Fact]
    public void Apply_RscExample_KeepsTextAndSpeaksDigits()
    {
        var c = Regex(@"RSC (\d+) (\d)/(\d)/(\d)", "RSC $1 $2/$3/$4", "RSC $digits($1) $digits($2) $digits($3) $digits($4)");

        Assert.Equal("RSC 23 5/5/5 WET", PatternMatcher.Apply("RSC 23 5/5/5 WET", c, voice: false));
        Assert.Equal(
            "RSC two three five five five WET",
            PatternMatcher.Apply("RSC 23 5/5/5 WET", c, voice: true));
    }

    [Fact]
    public void Apply_ApproachSuffixExample_SpeaksPhoneticForVoiceOnly()
    {
        var c = Regex(@"(ILS|RNAV) ([XYZ])\b", "$1 $2", "$1 $phonetic($2)");

        Assert.Equal("ILS Z RWY 28R", PatternMatcher.Apply("ILS Z RWY 28R", c, voice: false));
        Assert.Equal("ILS Zulu RWY 28R", PatternMatcher.Apply("ILS Z RWY 28R", c, voice: true));
    }

    [Theory]
    [InlineData("$group($1)", "five hundred and twenty-five")]
    [InlineData("$GROUP($1)", "five hundred and twenty-five")]
    [InlineData("$digits($1)", "five two five")]
    [InlineData("$phonetic($1)", "Foxtrot India Victor Echo Tango Whiskey Oscar Foxtrot India Victor Echo")]
    [InlineData("$spell($1)", "5 2 5")]
    [InlineData("$unknown($1)", "$unknown($1)")]
    [InlineData("$$1", "$1")]
    public void Apply_Functions(string voice, string expected)
    {
        var c = Regex(@"(\d+)FT", "$1", voice);
        Assert.Equal(expected, PatternMatcher.Apply("525FT", c, voice: true));
    }

    [Fact]
    public void Apply_PhoneticAndNamedGroups()
    {
        var c = Regex(@"(?<id>[A-Z]{2})", "$1", "$phonetic(${ID})");
        Assert.Equal("sierra kilo", PatternMatcher.Apply("SK", c, voice: true).ToLowerInvariant());
    }

    [Fact]
    public void Apply_InvalidPatternIsSkipped()
    {
        var c = Regex("(", "x", "x");
        Assert.Equal("ABC", PatternMatcher.Apply("ABC", c, voice: true));
    }

    [Theory]
    [InlineData("(", "x", "x", "Pattern", "Unclosed \"(\" at character 1")]
    [InlineData(@"RSC (\d+) (\d/(\d)/(\d)", "x", "x", "Pattern", "Unclosed \"(\" at character 11, near \"(\\d/(\\d)/(\\d)\". Add a \")\".")]
    [InlineData("a)", "x", "x", "Pattern", "Extra \")\" at character 2")]
    [InlineData("[a", "x", "x", "Pattern", "Unclosed \"[\" at character 1")]
    [InlineData("*a", "x", "x", "Pattern", "Nothing to repeat")]
    [InlineData("a*", "x", "x", "Pattern", "Matches empty text")]
    [InlineData("(a)", "$2", "x", "Text", "No group $2")]
        [InlineData("(a)", "x", "$1 ${name}", "Voice", "No group named 'name'")]
    [InlineData("(a)", "x", "$nope($1)", "Voice", "Unknown function '$nope'")]
    [InlineData("(a)", "x", "$spell", "Voice", "needs a group")]
    [InlineData("(a)", "x", "$spell(1)", "Voice", "needs a group")]
    [InlineData("a", "$1", "x", "Text", "No group $1")]
    public void Validate_ExplainsProblems(string pattern, string text, string voice, string field, string expected)
    {
        var errors = PatternMatcher.Validate(pattern, text, voice);
        var error = Assert.Single(errors, e => e.Field == field);
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void Apply_WholeMatchKeepsOriginalTextAndChangesVoice()
    {
        var c = Regex(@"\d{2}[LRC]", "$0", "RUNWAY $spell($0)");

        Assert.Equal("RWY 28R", PatternMatcher.Apply("RWY 28R", c, voice: false));
        Assert.Equal("RWY RUNWAY 2 8 R", PatternMatcher.Apply("RWY 28R", c, voice: true));
        Assert.Empty(PatternMatcher.Validate(@"\d{2}[LRC]", "$0", "RUNWAY $spell($0)"));
    }

    [Fact]
    public void Validate_AcceptsValidContractions()
    {
        Assert.Empty(PatternMatcher.Validate(@"(ILS|RNAV) ([XYZ])\b", "$1 $2", "$1 $PHONETIC($2)", requirePattern: true));
        Assert.Empty(PatternMatcher.Validate("(?<id>[A-Z]+)", "$1", "$spell(${ID})"));
    }

    [Fact]
    public void Validate_EmptyPatternOnlyReportedWhenRequired()
    {
        Assert.Empty(PatternMatcher.Validate("", "x", "x"));
        Assert.Contains(PatternMatcher.Validate("", "x", "x", requirePattern: true), e => e.Field == "Pattern");
    }

    [Fact]
    public void Clone_CopiesAllFields()
    {
        var clone = new PatternMeta { Text = "t", Voice = "v", Pattern = "p" }.Clone();
        Assert.Equal(("p", "t", "v"), (clone.Pattern, clone.Text, clone.Voice));
    }

    [Fact]
    public void TryPreview_ShowsTextAndVoiceResults()
    {
        var matched = PatternMatcher.TryPreview(
            @"(ILS|RNAV) ([XYZ])\b", "$1 $2", "$1 $phonetic($2)", "ILS Z RWY 28R", out var text, out var voice);

        Assert.True(matched);
        Assert.Equal("ILS Z RWY 28R", text);
        Assert.Equal("ILS Zulu RWY 28R", voice);
    }

    [Theory]
    [InlineData("", "ILS Z")]
    [InlineData("(", "ILS Z")]
    [InlineData("RNAV", "ILS Z")]
    [InlineData("ILS", "")]
    public void TryPreview_ReturnsFalseWithoutAMatch(string pattern, string sample)
    {
        Assert.False(PatternMatcher.TryPreview(pattern, "x", "x", sample, out var text, out var voice));
        Assert.Empty(text);
        Assert.Empty(voice);
    }

    [Theory]
    [InlineData("A25R", "$spell($1)", "A 2 5 R")]
    [InlineData("A25R", "$digits($1)", "A two five R")]
    [InlineData("25", "$phonetic($1)", "Tango Whiskey Oscar Foxtrot India Victor Echo")]
    [InlineData("2R", "$phonetic($1)", "Tango Whiskey Oscar Romeo")]
    public void Apply_SpellingFunctionsHandleDigitsAndLetters(string input, string voice, string expected)
    {
        var c = Regex(@"(\w+)", "$1", voice);
        Assert.Equal(expected, PatternMatcher.Apply(input, c, voice: true));
    }

    [Theory]
    [InlineData("25L", false, "two five left")]
    [InlineData("25R", false, "two five right")]
    [InlineData("18C", false, "one eight center")]
    [InlineData("36", false, "three six")]
    [InlineData("6L", false, "six left")]
    [InlineData("6L", true, "zero six left")]
    [InlineData("06L", false, "zero six left")]
    [InlineData("25l", false, "two five left")]
    [InlineData("25L/07R", false, "two five left/zero seven right")]
    [InlineData("RWY", false, "RWY")]
    [InlineData("99", false, "99")]
    public void Apply_RunwayFunction(string input, bool leadingZero, string expected)
    {
        var c = Regex(@"(.+)", "$1", "$runway($1)");

        Assert.Equal(expected, PatternMatcher.Apply(input, c, voice: true, runwayLeadingZero: leadingZero));
    }

    [Fact]
    public void Apply_RunwayFunctionInAPattern()
    {
        var c = Regex(@"RWY (\d{1,2}[LRC]?)", "RWY $1", "RUNWAY $runway($1)");

        Assert.Equal("LNDG RWY 25L", PatternMatcher.Apply("LNDG RWY 25L", c, voice: false));
        Assert.Equal("LNDG RUNWAY two five left", PatternMatcher.Apply("LNDG RWY 25L", c, voice: true));
    }
}
