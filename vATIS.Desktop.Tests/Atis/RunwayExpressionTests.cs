// <copyright file="RunwayExpressionTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Atis.Extensions;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class RunwayExpressionTests
{
    private static readonly ActiveRunways s_runways = new(["25L", "25R"], ["07"]);

    private static bool Eval(string condition, ActiveRunways? runways = null)
    {
        Assert.True(RunwayExpression.TryParse(condition, out var expression, out var error), error);
        return expression!.Evaluate(runways ?? s_runways);
    }

    [Fact]
    public void BareRunway_MatchesArrivalOrDeparture_IgnoringCase()
    {
        Assert.True(Eval("07"));
        Assert.True(Eval("25l"));
        Assert.False(Eval("16"));
    }

    [Fact]
    public void Prefix_RestrictsToArrivalOrDeparture()
    {
        Assert.True(Eval("dep:07"));
        Assert.False(Eval("arr:07"));
        Assert.True(Eval("arr:25L"));
        Assert.False(Eval("dep:25L"));
        Assert.True(Eval("DEPARTURE:07"));
        Assert.True(Eval("Arrival:25R"));
    }

    [Fact]
    public void Wildcard_MatchesSuffixes()
    {
        Assert.True(Eval("arr:25*"));
        Assert.False(Eval("arr:07*"));
        Assert.False(Eval("25"));
    }

    [Fact]
    public void NoRunways_NothingMatchesAndNotIsTrue()
    {
        Assert.False(Eval("07", ActiveRunways.None));
        Assert.True(Eval("NOT dep:07", ActiveRunways.None));
    }

    [Fact]
    public void ListAndOr_MeanAnyOf()
    {
        Assert.True(Eval("16 07"));
        Assert.True(Eval("16, dep:07"));
        Assert.True(Eval("16 OR arr:25L"));
        Assert.False(Eval("16 OR 34"));
    }

    [Fact]
    public void AndNot_AndParentheses()
    {
        Assert.True(Eval("dep:07 AND arr:25L"));
        Assert.True(Eval("dep:07 NOT arr:07"));
        Assert.False(Eval("NOT (dep:07 OR arr:25*)"));
        Assert.True(Eval("NOT (dep:25* OR arr:07)"));
    }

    [Fact]
    public void Split_AcceptsCommasSemicolonsAndSpaces()
    {
        Assert.Equal(["07L", "07R", "25"], ActiveRunways.Split("07L, 07R;25"));
        Assert.Empty(ActiveRunways.Split(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("07 AND")]
    [InlineData("(07")]
    [InlineData("07)")]
    [InlineData("OR 07")]
    [InlineData("dep:")]
    [InlineData("taxi:07")]
    public void Invalid_FailsToParse(string condition)
    {
        Assert.False(RunwayExpression.TryParse(condition, out var expression, out var error));
        Assert.Null(expression);
        Assert.False(string.IsNullOrEmpty(error));
    }
}
