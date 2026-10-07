using System.Collections.Generic;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class DatisTextProcessorTests
{
    private const string RenoBody =
        "RNO ATIS INFO Z 1555Z. 00000KT 10SM CLR 14/02 A3023 (THREE ZERO TWO THREE). VISUAL APCH IN USE. " +
        "LNDG AND DEPG RWY 17R. NOTICE TO AIRMEN. RWY 17L CLSD. TWY D BTWN 17R AND C CLSD. " +
        "CRANE .3 NAUTICAL MILES NW RNO AT 275 FEET ABOVE GROUND LEVEL. CAUTION, BIRDS NEAR AIRPORT. " +
        "...ADVS YOU HAVE INFO Z.";

    private readonly DatisTextProcessor _processor = new();

    private DatisResult Process(
        string body,
        List<DatisTextReplacement>? replacements = null,
        List<ContractionMeta>? contractions = null,
        string prependConditions = "",
        string appendConditions = "",
        string prependNotams = "",
        string appendNotams = "")
    {
        return _processor.Process(
            body,
            "station-1",
            'Z',
            contractions ?? [],
            replacements ?? [],
            prependConditions,
            appendConditions,
            prependNotams,
            appendNotams);
    }

    [Fact]
    public void Process_StripsEnvelopeAndTrailingAdvisory()
    {
        var result = Process(RenoBody);

        Assert.StartsWith("VISUAL APCH IN USE.", result.AirportConditions);
        Assert.DoesNotContain("RNO ATIS INFO", result.AirportConditions);
        Assert.DoesNotContain("00000KT", result.AirportConditions);
        Assert.DoesNotContain("ADVS YOU HAVE", result.Notams);
    }

    [Fact]
    public void Process_SplitsAirportConditionsAndNotams()
    {
        var result = Process(RenoBody);

        Assert.EndsWith("LNDG AND DEPG RWY 17R.", result.AirportConditions);
        Assert.StartsWith("RWY 17L CLSD.", result.Notams);
        Assert.EndsWith("CAUTION, BIRDS NEAR AIRPORT.", result.Notams);
    }

    [Fact]
    public void Process_PassesThroughStationIdAndLetter()
    {
        var result = Process(RenoBody);

        Assert.Equal("station-1", result.StationId);
        Assert.Equal('Z', result.AtisLetter);
    }

    [Theory]
    [InlineData("NOTICE TO AIR MISSIONS, NOTAMS. ")]
    [InlineData("NOTICE TO AIR MISSIONS. ")]
    [InlineData("NOTICE TO AIR MEN. ")]
    [InlineData("NOTICE TO AIRMEN. ")]
    [InlineData("NOTAMS. ")]
    [InlineData("NOTAM. ")]
    public void Process_NormalizesNotamHeaderVariants(string header)
    {
        var body = $"KXXX ATIS INFO A 1200Z. 27010KT 10SM CLR. VISUAL APCH IN USE. {header}RWY 1 CLSD. ...ADVS YOU HAVE INFO A.";

        var result = Process(body);

        Assert.Equal("VISUAL APCH IN USE.", result.AirportConditions);
        Assert.Equal("RWY 1 CLSD.", result.Notams);
    }

    [Fact]
    public void Process_WithoutNotamHeader_PutsEverythingInAirportConditions()
    {
        var result = Process("KXXX ATIS INFO A 1200Z. 27010KT 10SM CLR. VISUAL APCH IN USE. LNDG RWY 27. ...ADVS YOU HAVE INFO A.");

        Assert.Equal("VISUAL APCH IN USE. LNDG RWY 27.", result.AirportConditions);
        Assert.Equal(string.Empty, result.Notams);
    }

    [Fact]
    public void Process_RegexReplacement_RemovesMatchAndTrailingPunctuation()
    {
        var replacements = new List<DatisTextReplacement>
        {
            new()
            {
                Pattern = @"CRANE \.\d{1,2} NAUTICAL MILES \w+ RNO AT \d{1,5} FEET ABOVE GROUND LEVEL",
                Replacement = string.Empty,
                IsRegex = true,
            },
        };

        var result = Process(RenoBody, replacements);

        Assert.DoesNotContain("CRANE", result.Notams);
        Assert.Contains("TWY D BTWN 17R AND C CLSD. CAUTION, BIRDS NEAR AIRPORT.", result.Notams);
    }

    [Fact]
    public void Process_RegexReplacement_WithDoubledBackslashes_DoesNotMatch()
    {
        // "\\d" is a literal backslash followed by "d", so patterns saved with doubled backslashes never match.
        var replacements = new List<DatisTextReplacement>
        {
            new() { Pattern = @"CRANE \\.\\d{1,2}", Replacement = string.Empty, IsRegex = true },
        };

        var result = Process(RenoBody, replacements);

        Assert.Contains("CRANE", result.Notams);
    }

    [Fact]
    public void Process_PlainTextReplacement_ReplacesLiterally()
    {
        var replacements = new List<DatisTextReplacement>
        {
            new() { Pattern = "VISUAL APCH", Replacement = "VISUAL APPROACH", IsRegex = false },
        };

        var result = Process(RenoBody, replacements);

        Assert.Contains("VISUAL APPROACH IN USE", result.AirportConditions);
    }

    [Fact]
    public void Process_PlainTextReplacement_DoesNotTreatPatternAsRegex()
    {
        var replacements = new List<DatisTextReplacement>
        {
            new() { Pattern = @"\d+", Replacement = "X", IsRegex = false },
        };

        var result = Process(RenoBody, replacements);

        Assert.Contains("RWY 17R", result.AirportConditions);
    }

    [Fact]
    public void Process_NullReplacementText_IsTreatedAsEmpty()
    {
        var replacements = new List<DatisTextReplacement>
        {
            new() { Pattern = "VISUAL APCH IN USE", Replacement = null!, IsRegex = true },
            new() { Pattern = "RWY 17L CLSD", Replacement = null!, IsRegex = false },
        };

        var result = Process(RenoBody, replacements);

        Assert.DoesNotContain("VISUAL APCH", result.AirportConditions);
        Assert.DoesNotContain("RWY 17L CLSD", result.Notams);
    }

    [Fact]
    public void Process_InvalidRegex_IsSkippedAndOthersStillApply()
    {
        var replacements = new List<DatisTextReplacement>
        {
            new() { Pattern = "([unclosed", Replacement = "X", IsRegex = true },
            new() { Pattern = "VISUAL APCH", Replacement = "VISUAL APPROACH", IsRegex = false },
        };

        var result = Process(RenoBody, replacements);

        Assert.Contains("VISUAL APPROACH IN USE", result.AirportConditions);
    }

    [Fact]
    public void Process_EmptyPattern_IsIgnored()
    {
        var replacements = new List<DatisTextReplacement>
        {
            new() { Pattern = string.Empty, Replacement = "X", IsRegex = false },
        };

        var result = Process(RenoBody, replacements);

        Assert.Equal(Process(RenoBody), result);
    }

    [Fact]
    public void Process_AppliesPrependAndAppendToEachSection()
    {
        var result = Process(
            RenoBody,
            prependConditions: "  PRE-AC ",
            appendConditions: " POST-AC",
            prependNotams: "PRE-NOTAM",
            appendNotams: "POST-NOTAM  ");

        Assert.StartsWith("PRE-AC VISUAL APCH IN USE.", result.AirportConditions);
        Assert.EndsWith("RWY 17R. POST-AC", result.AirportConditions);
        Assert.StartsWith("PRE-NOTAM RWY 17L CLSD.", result.Notams);
        Assert.EndsWith("AIRPORT. POST-NOTAM", result.Notams);
    }

    [Fact]
    public void Process_BlankPrependAndAppend_AreIgnored()
    {
        var plain = Process(RenoBody);
        var blank = Process(RenoBody, prependConditions: "   ", appendNotams: " ");

        Assert.Equal(plain, blank);
    }

    [Fact]
    public void Process_ExpandsContractionsToTemplateVariables()
    {
        var contractions = new List<ContractionMeta>
        {
            new() { VariableName = "runway", Text = "RWY", Voice = "runway" },
            new() { VariableName = "closed", Text = "CLSD", Voice = "closed" },
        };

        var result = Process(RenoBody, contractions: contractions);

        Assert.Contains("@runway 17R", result.AirportConditions);
        Assert.Contains("@runway 17L @closed.", result.Notams);
    }

    [Fact]
    public void Process_ContractionsOnlyMatchWholeWords()
    {
        var contractions = new List<ContractionMeta>
        {
            new() { VariableName = "tw", Text = "TW", Voice = "taxiway" },
        };

        var result = Process(RenoBody, contractions: contractions);

        Assert.DoesNotContain("@tw", result.Notams);
        Assert.Contains("TWY D", result.Notams);
    }

    [Fact]
    public void Process_LongerContractionsTakePrecedence()
    {
        var contractions = new List<ContractionMeta>
        {
            new() { VariableName = "short", Text = "BIRDS", Voice = "birds" },
            new() { VariableName = "long", Text = "BIRDS NEAR", Voice = "birds near" },
        };

        var result = Process(RenoBody, contractions: contractions);

        Assert.Contains("@long AIRPORT", result.Notams);
        Assert.DoesNotContain("@short", result.Notams);
    }

    [Fact]
    public void Process_SkipsDigitOnlyAndIncompleteContractions()
    {
        var contractions = new List<ContractionMeta>
        {
            new() { VariableName = "num", Text = "17", Voice = "one seven" },
            new() { VariableName = null, Text = "CLSD", Voice = "closed" },
            new() { VariableName = "blank", Text = null, Voice = "x" },
        };

        var result = Process(RenoBody, contractions: contractions);

        Assert.DoesNotContain("@", result.AirportConditions);
        Assert.DoesNotContain("@", result.Notams);
    }

    [Fact]
    public void Process_CleansUpDoublePeriodsAndKeepsEllipses()
    {
        var body = "KXXX ATIS INFO A 1200Z. 27010KT 10SM CLR. LNDG RWY 27.. VISUAL APCH IN USE... NOTAMS. RWY 1 CLSD.. ...ADVS YOU HAVE INFO A.";

        var result = Process(body);

        Assert.Equal("LNDG RWY 27. VISUAL APCH IN USE...", result.AirportConditions);
        Assert.Equal("RWY 1 CLSD.", result.Notams);
    }
}
