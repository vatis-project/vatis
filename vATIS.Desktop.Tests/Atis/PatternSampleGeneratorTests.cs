using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class PatternSampleGeneratorTests
{
    [Theory]
    [InlineData(@"(ILS|RNAV) ([XYZ])\b")]
    [InlineData(@"RSC (\d+) (\d)/(\d)/(\d)")]
    [InlineData(@"(\d{4})FT")]
    [InlineData(@"\bPAPI\b")]
    [InlineData(@"(?<id>[A-Z]{2})-\k<id>")]
    [InlineData(@"(?i)ils [xyz]")]
    [InlineData(@"RWY (\d{2})[LRC]?")]
    [InlineData(@"[^0-9]{2}\d")]
    [InlineData(@"\d{2,4}(FT|M)")]
    [InlineData(@"(a|b)+c*")]
    [InlineData(@"^TWY ([A-Z])(\d)?$")]
    [InlineData(@"(?:CLSD|OTS) (?=\d)\d+")]
    [InlineData(@"\w+ \w+")]
    [InlineData(@"RWY\s\d{1,2}[LRC]{0,1}")]
    public void TryGenerate_ProducesTextTheMatchedPatternAccepts(string pattern)
    {
        for (var seed = 0; seed < 25; seed++)
        {
            Assert.True(PatternSampleGenerator.TryGenerate(pattern, new Random(seed), out var sample), pattern);
            Assert.Matches(new Regex(pattern), sample);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("(")]
    [InlineData("[a")]
    [InlineData("a)")]
    [InlineData(@"\")]
    public void TryGenerate_FailsForInvalidPatterns(string pattern)
    {
        Assert.False(PatternSampleGenerator.TryGenerate(pattern, new Random(1), out var sample));
        Assert.Empty(sample);
    }

    [Fact]
    public void SeededFor_GivesTheSameSampleForTheSamePattern()
    {
        const string pattern = @"RSC (\d+) (\d)/(\d)/(\d)";

        PatternSampleGenerator.TryGenerate(pattern, PatternSampleGenerator.SeededFor(pattern), out var first);
        PatternSampleGenerator.TryGenerate(pattern, PatternSampleGenerator.SeededFor(pattern), out var second);

        Assert.Equal(first, second);
    }
}
