using System.Collections.Generic;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class ContractionMatcherTests
{
    private static List<ContractionMeta> Regex(string pattern, string text, string voice) =>
        [new ContractionMeta { VariableName = "X", IsRegex = true, Pattern = pattern, Text = text, Voice = voice }];

    [Fact]
    public void Apply_RscExample_KeepsTextAndSpeaksDigits()
    {
        var c = Regex(@"RSC (\d+) (\d)/(\d)/(\d)", "RSC $1 $2/$3/$4", "RSC $digits($1) $digits($2) $digits($3) $digits($4)");

        Assert.Equal("RSC 23 5/5/5 WET", ContractionMatcher.Apply("RSC 23 5/5/5 WET", c, voice: false));
        Assert.Equal(
            "RSC two three five five five WET",
            ContractionMatcher.Apply("RSC 23 5/5/5 WET", c, voice: true));
    }

    [Fact]
    public void Apply_ApproachSuffixExample_SpeaksPhoneticForVoiceOnly()
    {
        var c = Regex(@"(ILS|RNAV) ([XYZ])\b", "$1 $2", "$1 $phonetic($2)");

        Assert.Equal("ILS Z RWY 28R", ContractionMatcher.Apply("ILS Z RWY 28R", c, voice: false));
        Assert.Equal("ILS Zulu RWY 28R", ContractionMatcher.Apply("ILS Z RWY 28R", c, voice: true));
    }

    [Theory]
    [InlineData("$group($1)", "five hundred and twenty-five")]
    [InlineData("$GROUP($1)", "five hundred and twenty-five")]
    [InlineData("$digits($1)", "five two five")]
    [InlineData("$phonetic($1)", "five two five")]
    [InlineData("$unknown($1)", "$unknown($1)")]
    [InlineData("$$1", "$1")]
    public void Apply_Functions(string voice, string expected)
    {
        var c = Regex(@"(\d+)FT", "$1", voice);
        Assert.Equal(expected, ContractionMatcher.Apply("525FT", c, voice: true));
    }

    [Fact]
    public void Apply_PhoneticAndNamedGroups()
    {
        var c = Regex(@"(?<id>[A-Z]{2})", "$1", "$phonetic(${ID})");
        Assert.Equal("sierra kilo", ContractionMatcher.Apply("SK", c, voice: true).ToLowerInvariant());
    }

    [Fact]
    public void Apply_InvalidPatternIsSkipped()
    {
        var c = Regex("(", "x", "x");
        Assert.Equal("ABC", ContractionMatcher.Apply("ABC", c, voice: true));
    }

    [Fact]
    public void Apply_IgnoresNonRegexContractions()
    {
        var c = new List<ContractionMeta> { new() { VariableName = "A", Pattern = "ABC", Text = "x", Voice = "x" } };
        Assert.Equal("ABC", ContractionMatcher.Apply("ABC", c, voice: true));
    }

    [Theory]
    [InlineData("(", "x", "x", "Pattern", "Unclosed \"(\" at character 1")]
    [InlineData(@"RSC (\d+) (\d/(\d)/(\d)", "x", "x", "Pattern", "Unclosed \"(\" at character 11, near \"(\\d/(\\d)/(\\d)\". Add a \")\".")]
    [InlineData("a)", "x", "x", "Pattern", "Extra \")\" at character 2")]
    [InlineData("[a", "x", "x", "Pattern", "Unclosed \"[\" at character 1")]
    [InlineData("*a", "x", "x", "Pattern", "Nothing to repeat")]
    [InlineData("a*", "x", "x", "Pattern", "Matches empty text")]
    [InlineData("(a)", "$2", "x", "Text", "No group $2")]
    [InlineData("(a)", "$0", "x", "Text", "There is no $0")]
    [InlineData("(a)", "x", "$1 ${name}", "Voice", "No group named 'name'")]
    [InlineData("(a)", "x", "$nope($1)", "Voice", "Unknown function '$nope'")]
    [InlineData("(a)", "x", "$spell", "Voice", "needs a group")]
    [InlineData("(a)", "x", "$spell(1)", "Voice", "needs a group")]
    [InlineData("a", "$1", "x", "Text", "No group $1")]
    public void Validate_ExplainsProblems(string pattern, string text, string voice, string field, string expected)
    {
        var errors = ContractionMatcher.Validate(pattern, text, voice);
        var error = Assert.Single(errors, e => e.Field == field);
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void Validate_AcceptsValidContractions()
    {
        Assert.Empty(ContractionMatcher.Validate(@"(ILS|RNAV) ([XYZ])\b", "$1 $2", "$1 $PHONETIC($2)", requirePattern: true));
        Assert.Empty(ContractionMatcher.Validate("(?<id>[A-Z]+)", "$1", "$spell(${ID})"));
    }

    [Fact]
    public void Validate_EmptyPatternOnlyReportedWhenRequired()
    {
        Assert.Empty(ContractionMatcher.Validate("", "x", "x"));
        Assert.Contains(ContractionMatcher.Validate("", "x", "x", requirePattern: true), e => e.Field == "Pattern");
    }

    [Fact]
    public void Clone_CopiesAllFields()
    {
        var clone = new ContractionMeta { VariableName = "V", Text = "t", Voice = "v", IsRegex = true, Pattern = "p" }.Clone();
        Assert.Equal(("V", "t", "v", true, "p"), (clone.VariableName, clone.Text, clone.Voice, clone.IsRegex, clone.Pattern));
    }
}
