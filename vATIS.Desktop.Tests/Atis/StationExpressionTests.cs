// <copyright file="StationExpressionTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Linq;
using System.Text.Json;
using Vatsim.Vatis;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class StationExpressionTests
{
    private static bool Eval(string text, params string[] online)
    {
        Assert.True(StationExpression.TryParse(text, out var expression, out var error), error);
        return expression!.Evaluate(online);
    }

    [Theory]
    [InlineData("EPWA_APP, EPWW_*_CTR", "EPWA_APP", true)]
    [InlineData("EPWA_APP EPWW_*_CTR", "EPWW_W_CTR", true)]
    [InlineData("epwa_app", "EPWA_APP", true)]
    [InlineData("EPWA_APP, EPWW_*_CTR", "EPWA_TWR", false)]
    public void PlainList_IsAnyOf(string text, string online, bool expected)
    {
        Assert.Equal(expected, Eval(text, online));
    }

    [Theory]
    [InlineData("LAS_E_GND NOT LAS_W_GND", new[] { "LAS_E_GND" }, true)]
    [InlineData("LAS_E_GND NOT LAS_W_GND", new[] { "LAS_E_GND", "LAS_W_GND" }, false)]
    [InlineData("LAS_E_GND NOT LAS_W_GND", new[] { "LAS_W_GND" }, false)]
    [InlineData("LAS_E_GND AND NOT LAS_W_GND", new[] { "LAS_E_GND" }, true)]
    [InlineData("LAS_E_GND and LAS_W_GND", new[] { "LAS_E_GND" }, false)]
    [InlineData("LAS_E_GND and LAS_W_GND", new[] { "LAS_E_GND", "LAS_W_GND" }, true)]
    [InlineData("LAS_E_GND OR LAS_W_GND", new[] { "LAS_W_GND" }, true)]
    [InlineData("NOT LAS_W_GND", new string[0], true)]
    [InlineData("NOT NOT LAS_W_GND", new[] { "LAS_W_GND" }, true)]
    public void Operators(string text, string[] online, bool expected)
    {
        Assert.Equal(expected, Eval(text, online));
    }

    [Fact]
    public void AndBindsTighterThanOr()
    {
        Assert.True(Eval("A AND B OR C", "C"));
        Assert.False(Eval("A OR B AND C", "B"));
    }

    [Fact]
    public void ImplicitOr_IsLowestPrecedence()
    {
        Assert.True(Eval("A AND B C", "C"));
        Assert.False(Eval("A AND B C", "A"));
    }

    [Fact]
    public void Parentheses_ChangeTheResult()
    {
        Assert.False(Eval("(A OR B) AND C", "A"));
        Assert.True(Eval("(A OR B) AND C", "A", "C"));
        Assert.True(Eval("(A B) NOT (C D)", "B"));
        Assert.False(Eval("(A B) NOT (C D)", "B", "D"));
    }

    [Theory]
    [InlineData("LAS_H1_TWR", true)]
    [InlineData("LAS_H_TWR", true)]
    [InlineData("las_hx_twr", true)]
    [InlineData("LAS_H_GND", false)]
    [InlineData("LAX_H1_TWR", false)]
    public void Wildcard_MatchesTowerNotGround(string online, bool expected)
    {
        Assert.Equal(expected, Eval("LAS_H*_TWR", online));
    }

    [Fact]
    public void Wildcards_WorkInsideExpressions()
    {
        Assert.True(Eval("EPWW_*_CTR NOT EPWA_*", "EPWW_W_CTR"));
        Assert.False(Eval("EPWW_*_CTR NOT EPWA_*", "EPWW_W_CTR", "EPWA_APP"));
    }

    [Fact]
    public void UnknownFeed_TreatedAsNobodyOnline()
    {
        StationExpression.TryParse("NOT A", out var expression, out _);
        Assert.True(expression!.Evaluate(null));
    }

    [Theory]
    [InlineData("", "Enter at least one callsign.")]
    [InlineData("A AND", "ends too soon")]
    [InlineData("A OR OR B", "not 'OR'")]
    [InlineData("(A B", "Missing a closing ')'")]
    [InlineData("A B)", "without a matching '('")]
    [InlineData("NOT", "ends too soon")]
    [InlineData("()", "not ')'")]
    public void InvalidExpressions_ReportAClearError(string text, string expectedMessage)
    {
        Assert.False(StationExpression.TryParse(text, out var expression, out var error));
        Assert.Null(expression);
        Assert.Contains(expectedMessage, error);
    }

    [Fact]
    public void Errors_IncludeAPosition()
    {
        StationExpression.TryParse("A AND OR", out _, out var error);
        Assert.Contains("(position 7)", error);
    }

    [Fact]
    public void Filter_AppliesTheOperators()
    {
        var online = new StaticDefinition("GC COMBINED ON 121.1", 1) { OnlineCondition = "LAS_E_GND NOT LAS_W_GND" };
        var offline = new StaticDefinition("USE WEST GROUND", 2) { OnlineCondition = "NOT (LAS_E_GND NOT LAS_W_GND)" };

        string[] Active(params string[] callsigns) =>
            StaticDefinitionFilter.GetActive([online, offline], callsigns).Select(x => x.Text).ToArray();

        Assert.Equal(["GC COMBINED ON 121.1"], Active("LAS_E_GND"));
        Assert.Equal(["USE WEST GROUND"], Active("LAS_E_GND", "LAS_W_GND"));
        Assert.Equal(["USE WEST GROUND"], Active());
    }

    [Fact]
    public void Filter_InvalidStationConditionIsNeverMet()
    {
        var bad = new StaticDefinition("A", 1) { OnlineCondition = "(A" };
        Assert.Empty(StaticDefinitionFilter.GetActive([bad], ["A"]));
        Assert.Empty(StaticDefinitionFilter.GetActive([bad], []));
    }

    [Theory]
    [InlineData(StationCondition.Always, "EPWA_APP", null)]
    [InlineData(StationCondition.WhenOnline, "  ", null)]
    [InlineData(StationCondition.WhenOnline, "EPWA_APP", "EPWA_APP")]
    [InlineData(StationCondition.WhenOnline, "epwa_app, EPWW_*_CTR;X_TWR", "epwa_app OR EPWW_*_CTR OR X_TWR")]
    [InlineData(StationCondition.WhenOffline, "EPWA_DEL", "NOT EPWA_DEL")]
    [InlineData(StationCondition.WhenOffline, "EPWA_DEL EPWA_APP", "NOT (EPWA_DEL OR EPWA_APP)")]
    public void FromLegacy_ConvertsTheOldCondition(StationCondition condition, string callsigns, string? expected)
    {
        Assert.Equal(expected, StationExpression.FromLegacy(condition, callsigns));
    }

    [Theory]
    [InlineData(StationCondition.WhenOnline, "EPWA_APP, EPWW_*_CTR")]
    [InlineData(StationCondition.WhenOffline, "EPWA_APP, EPWW_*_CTR")]
    public void FromLegacy_KeepsTheOldMeaning(StationCondition condition, string callsigns)
    {
        var converted = StationExpression.FromLegacy(condition, callsigns);
        Assert.True(StationExpression.TryParse(converted, out var expression, out _));
        foreach (var online in new[] { new string[0], ["EPWA_APP"], ["EPWW_E_CTR"], ["EPWA_TWR"], ["EPWA_APP", "EPWA_TWR"] })
        {
            var any = online.Any(x => x is "EPWA_APP" || x.StartsWith("EPWW_") && x.EndsWith("_CTR"));
            Assert.Equal(condition == StationCondition.WhenOnline ? any : !any, expression!.Evaluate(online));
        }
    }

    [Fact]
    public void OldProfileJson_IsConvertedWhenLoaded_AndWrittenWithTheNewProperty()
    {
        const string json = """
            {"identifier":"EPWA","name":"W","airportConditionDefinitions":[
              {"text":"A","ordinal":1,"enabled":true,"stationCondition":2,"stationCallsigns":"EPWA_DEL, EPWA_APP"}],
             "notamDefinitions":[
              {"text":"B","ordinal":1,"enabled":true,"onlineCondition":"LAS_E_GND NOT LAS_W_GND"}]}
            """;
        var station = JsonSerializer.Deserialize(json, SourceGenerationContext.NewDefault.AtisStation)!;
        var loaded = station.AirportConditionDefinitions.Single();
        Assert.Equal("NOT (EPWA_DEL OR EPWA_APP)", loaded.OnlineCondition);
        Assert.Equal(StationCondition.Always, loaded.StationCondition);
        Assert.Null(loaded.StationCallsigns);
        Assert.Equal("LAS_E_GND NOT LAS_W_GND", station.NotamDefinitions.Single().OnlineCondition);

        var written = JsonSerializer.Serialize(station, SourceGenerationContext.NewDefault.AtisStation);
        Assert.DoesNotContain("stationCondition", written);
        Assert.DoesNotContain("stationCallsigns", written);
        Assert.Contains("\"onlineCondition\": \"NOT (EPWA_DEL OR EPWA_APP)\"", written);
    }

    private static bool EvalAt(string text, string time, params string[] online)
    {
        Assert.True(StationExpression.TryParse(text, out var expression, out var error), error);
        var parts = time.Split(':');
        var now = new DateTime(2026, 10, 24, int.Parse(parts[0]), int.Parse(parts[1]), 0, DateTimeKind.Utc);
        return expression!.Evaluate(online, now);
    }

    [Theory]
    [InlineData("0700z-1400z", "07:00", true)]
    [InlineData("0700z-1400z", "10:30", true)]
    [InlineData("0700z-1400z", "13:59", true)]
    [InlineData("0700z-1400z", "14:00", false)]
    [InlineData("0700z-1400z", "06:59", false)]
    [InlineData("0700z - 1400z", "10:00", true)]
    [InlineData("0700Z-1400Z", "10:00", true)]
    [InlineData("0700-1400", "10:00", true)]
    [InlineData("0700 - 1400", "14:00", false)]
    public void TimeRange_IsFromStartUpToButNotIncludingEnd(string text, string time, bool expected)
    {
        Assert.Equal(expected, EvalAt(text, time));
    }

    [Theory]
    [InlineData("2200z-0600z", "22:00", true)]
    [InlineData("2200z-0600z", "23:59", true)]
    [InlineData("2200z-0600z", "00:00", true)]
    [InlineData("2200z-0600z", "05:59", true)]
    [InlineData("2200z-0600z", "06:00", false)]
    [InlineData("2200z-0600z", "12:00", false)]
    [InlineData("2200z-0600z", "21:59", false)]
    public void TimeRange_WrapsPastMidnight(string text, string time, bool expected)
    {
        Assert.Equal(expected, EvalAt(text, time));
    }

    [Fact]
    public void TimeRange_CanRunToMidnight()
    {
        Assert.True(EvalAt("2000z-2400z", "23:59"));
        Assert.False(EvalAt("2000z-2400z", "00:00"));
        Assert.True(EvalAt("0000z-2400z", "12:00"));
    }

    [Fact]
    public void TimeRange_CombinesWithCallsigns()
    {
        const string text = "LAX_TWR AND 0700z-1400z";
        Assert.True(EvalAt(text, "10:00", "LAX_TWR"));
        Assert.False(EvalAt(text, "10:00"));
        Assert.False(EvalAt(text, "15:00", "LAX_TWR"));

        Assert.True(EvalAt("NOT 0700z-1400z", "15:00"));
        Assert.True(EvalAt("2200z-0600z OR LAX_GND", "12:00", "LAX_GND"));
        Assert.True(EvalAt("(LAX_TWR OR LAX_GND) AND 2200z-0600z", "23:00", "LAX_GND"));
        Assert.True(EvalAt("LAX_TWR NOT 0700z-1400z", "15:00", "LAX_TWR"));
    }

    [Fact]
    public void HasTimeCondition_IsOnlySetWhenATimeRangeIsUsed()
    {
        StationExpression.TryParse("LAX_TWR AND 0700z-1400z", out var withTime, out _);
        StationExpression.TryParse("LAX_TWR OR LAX_GND", out var without, out _);
        Assert.True(withTime!.HasTimeCondition);
        Assert.False(without!.HasTimeCondition);
    }

    [Theory]
    [InlineData("2560z-1400z", "not a valid time")]
    [InlineData("0700z-1460z", "not a valid time")]
    [InlineData("2401z-0600z", "not a valid time")]
    [InlineData("0700z-0700z", "are the same")]
    [InlineData("0700z", "Write a time as a range")]
    [InlineData("LAX_TWR AND 0700", "Write a time as a range")]
    [InlineData("0700z-", "Write a time as a range")]
    public void InvalidTimes_ReportAClearError(string text, string expectedMessage)
    {
        Assert.False(StationExpression.TryParse(text, out _, out var error));
        Assert.Contains(expectedMessage, error);
    }

    [Fact]
    public void Filter_UsesTheGivenTime()
    {
        var def = new StaticDefinition("NOISE ABATEMENT PROCEDURES IN EFFECT", 1) { OnlineCondition = "2200z-0600z" };
        var at = (int hour) => StaticDefinitionFilter
            .GetActive([def], null, utcNow: new DateTime(2026, 10, 24, hour, 0, 0, DateTimeKind.Utc)).Count;
        Assert.Equal(1, at(23));
        Assert.Equal(0, at(12));
        Assert.True(StaticDefinitionFilter.HasTimeConditions([def]));
        Assert.False(StaticDefinitionFilter.HasTimeConditions(
            [new StaticDefinition("A", 1) { OnlineCondition = "LAX_TWR" }]));
        Assert.False(StaticDefinitionFilter.HasTimeConditions(
            [new StaticDefinition("A", 1, enabled: false) { OnlineCondition = "2200z-0600z" }]));
    }
}
