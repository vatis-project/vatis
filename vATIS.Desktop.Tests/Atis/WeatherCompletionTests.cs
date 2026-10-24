// <copyright file="WeatherCompletionTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Linq;
using Vatsim.Vatis.Atis.Extensions;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class WeatherCompletionTests
{
    // "|" marks the caret.
    private static CompletionSuggestionResult At(string textWithCaret)
    {
        var caret = textWithCaret.IndexOf('|');
        return WeatherCompletion.GetSuggestions(textWithCaret.Remove(caret, 1), caret);
    }

    private static string[] Names(CompletionSuggestionResult result) => result.Items.Select(x => x.Text).ToArray();

    [Fact]
    public void EmptyText_SuggestsFieldsAndNot()
    {
        var names = Names(At("|"));
        Assert.Contains("ceiling", names);
        Assert.Contains("rvr", names);
        Assert.Contains("cavok", names);
        Assert.Contains("NOT", names);
        Assert.Equal(16, names.Length);
    }

    [Fact]
    public void PartialWord_FiltersFieldsAndReportsWhereItStarts()
    {
        var result = At("ceiling < 200ft AND vi|");
        Assert.Equal(["visibility"], Names(result));
        Assert.Equal("ceiling < 200ft AND ".Length, result.StartOffset);
    }

    [Fact]
    public void ExactWordIsStillOffered_SoItCanBeAcceptedWithEnter()
    {
        Assert.Equal(["rvr"], Names(At("rvr|")));
        Assert.Equal(["cloudbase", "cloudtype"], Names(At("cloud|")).OrderBy(x => x));
    }

    [Theory]
    [InlineData("ceiling |")]
    [InlineData("rvr |")]
    [InlineData("NOT visibility |")]
    public void AfterANumericField_SuggestsComparisons(string text)
    {
        Assert.Equal(["<", "<=", ">", ">=", "="], Names(At(text)));
    }

    [Fact]
    public void AfterATextField_SuggestsIncludesAndExcludes()
    {
        Assert.Equal(["INCLUDES", "EXCLUDES"], Names(At("weather |")));
        Assert.Equal(["INCLUDES"], Names(At("cloudtype inc|")));
    }

    [Fact]
    public void AfterTheOperator_ANumberIsExpectedSoNothingIsSuggested()
    {
        Assert.Empty(Names(At("ceiling < |")));
        Assert.Empty(Names(At("ceiling <= x|")));
    }

    [Fact]
    public void RightAfterDigits_UnitsAreOffered()
    {
        Assert.Equal(["ft", "m", "AND", "OR"], Names(At("ceiling <= 2|")));
    }

    [Fact]
    public void AfterANumber_SuggestsUnitsForThatFieldAndJoiners()
    {
        var names = Names(At("ceiling < 200 |"));
        Assert.Equal(["ft", "m", "AND", "OR"], names);
        Assert.Equal(["kt", "kmh", "mps", "AND", "OR"], Names(At("windspeed > 20 |")));
        Assert.Contains("c", Names(At("temperature < 0 |")));
        Assert.DoesNotContain("ft", Names(At("temperature < 0 |")));
    }

    [Fact]
    public void UnitDirectlyAfterANumber_IsCompletedInPlace()
    {
        var result = At("rvr < 550k|");
        Assert.Equal(["km"], Names(result));
        Assert.Equal("rvr < 550".Length, result.StartOffset);
    }

    [Theory]
    [InlineData("rvr < 500m|", "m", "rvr < 500")]
    [InlineData("rvr < 500 m|", "m", "rvr < 500 ")]
    [InlineData("ceiling < 200ft|", "ft", "ceiling < 200")]
    [InlineData("windspeed > 20kt|", "kt", "windspeed > 20")]
    [InlineData("temperature < -2c|", "c", "temperature < -2")]
    public void ATypedUnitIsOfferedSoItCanBeAccepted(string text, string unit, string before)
    {
        var result = At(text);
        Assert.Equal(unit, result.Items.First().Text);
        Assert.Equal(unit + " ", result.Items.First().InsertText);
        Assert.Equal(before.Length, result.StartOffset);
    }

    [Fact]
    public void TypingMAfterANumber_OffersMeters()
    {
        // "rvr < 500m": m is a unit for rvr, and km is not offered because it does not start with m.
        Assert.Equal(["m"], Names(At("rvr < 500m|")));
        Assert.Equal(["mm"], Names(At("depth < 3m|")));
        Assert.Equal(["m"], Names(At("visibility < 800m|")));
    }

    [Fact]
    public void AfterACompleteCondition_SuggestsJoiners()
    {
        Assert.Equal(["AND", "OR"], Names(At("ceiling < 200ft |")));
        Assert.Equal(["AND", "OR"], Names(At("windshear |")));
        Assert.Equal(["AND", "OR"], Names(At("(cavok) |")));
        Assert.Equal(["AND"], Names(At("cavok a|")));
    }

    [Fact]
    public void AfterAndOrNotOrParenthesis_SuggestsFields()
    {
        Assert.Contains("gust", Names(At("cavok AND |")));
        Assert.Contains("gust", Names(At("cavok or |")));
        Assert.Contains("gust", Names(At("NOT |")));
        Assert.Contains("gust", Names(At("(|")));
        Assert.Contains("gust", Names(At("cavok AND (g|")));
    }

    [Fact]
    public void AfterIncludes_SuggestsCodesForTheField()
    {
        Assert.Contains("FZRA", Names(At("weather INCLUDES |")));
        Assert.Equal(["CB", "TCU"], Names(At("cloudtype includes |")));
        Assert.Equal(["TS", "TSRA"], Names(At("weather includes T|")));
        Assert.All(At("weather includes |").Items, x => Assert.False(x.ContinueAfter));
    }

    [Fact]
    public void InsideQuotes_SuggestsCodesOneWordAtATime()
    {
        var result = At("weather includes \"FZRA S|");
        Assert.Contains("SN", Names(result));
        Assert.DoesNotContain("FZRA", Names(result));
        Assert.Equal("weather includes \"FZRA ".Length, result.StartOffset);
        Assert.Equal(["CB"], Names(At("cloudtype includes \"C|")));
    }

    [Fact]
    public void AfterTheCodes_SuggestsJoinersAgain()
    {
        Assert.Equal(["AND", "OR"], Names(At("weather includes \"TS FG\" |")));
        Assert.Equal(["AND", "OR"], Names(At("weather includes TS |")));
    }

    [Fact]
    public void UnknownFieldOrJunk_SuggestsNothing()
    {
        Assert.Empty(Names(At("height |")));
        Assert.Empty(Names(At("ceiling < 200ft ceiling |")));
    }

    [Fact]
    public void CaretInTheMiddle_UsesOnlyTheTextBeforeIt()
    {
        var result = At("ceil| < 200ft AND rvr < 550m");
        Assert.Equal(["ceiling"], Names(result));
        Assert.Equal(0, result.StartOffset);
    }

    [Fact]
    public void InsertTextAddsASpaceWhereAnotherPieceFollows()
    {
        var field = At("cei|").Items.Single();
        Assert.Equal("ceiling ", field.InsertText);
        Assert.True(field.ContinueAfter);

        var op = At("ceiling |").Items.First();
        Assert.Equal("< ", op.InsertText);
        Assert.False(op.ContinueAfter);
    }

    [Fact]
    public void OutOfRangeCaret_IsClamped()
    {
        Assert.NotNull(WeatherCompletion.GetSuggestions("ceiling ", 999));
        Assert.NotEmpty(WeatherCompletion.GetSuggestions(null, 5).Items);
    }
}
