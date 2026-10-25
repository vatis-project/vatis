// <copyright file="StationCompletionTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Linq;
using Vatsim.Vatis.Atis.Extensions;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class StationCompletionTests
{
    private static string[] Suggest(string text)
    {
        return StationCompletion.GetSuggestions(text, text.Length).Items.Select(x => x.Text).ToArray();
    }

    [Theory]
    [InlineData("")]
    [InlineData("LAX_GND AND ")]
    [InlineData("LAX_GND or ")]
    [InlineData("LAX_GND AND NOT ")]
    [InlineData("(")]
    [InlineData("LAX_GND, ")]
    public void WhereACallsignIsExpected_OnlyNotIsOffered(string text)
    {
        Assert.Equal(["NOT"], Suggest(text));
    }

    [Theory]
    [InlineData("LAX_GND ")]
    [InlineData("(LAX_GND OR LAX_TWR) ")]
    [InlineData("LAX_GND AND NOT LAX_DEL ")]
    public void AfterACallsign_OperatorsAreOffered(string text)
    {
        Assert.Equal(["AND", "OR", "NOT"], Suggest(text));
    }

    [Fact]
    public void ThePartialWord_FiltersAndSetsTheStart()
    {
        var result = StationCompletion.GetSuggestions("LAX_GND a", 9);
        Assert.Equal(8, result.StartOffset);
        Assert.Equal(["AND"], result.Items.Select(x => x.Text).ToArray());

        Assert.Equal(["OR"], Suggest("LAX_GND o"));
        Assert.Empty(Suggest("LAX_GND x"));
    }

    [Fact]
    public void WhileTypingTheFirstCallsign_OnlyAMatchingNotIsOffered()
    {
        Assert.Empty(Suggest("LAX"));
        Assert.Equal(["NOT"], Suggest("no"));
    }
}
