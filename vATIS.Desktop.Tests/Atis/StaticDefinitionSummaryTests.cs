// <copyright file="StaticDefinitionSummaryTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class StaticDefinitionSummaryTests
{
    [Fact]
    public void AnUnconditionalDefinition_HasNoLines()
    {
        Assert.Empty(StaticDefinitionSummary.Describe(new StaticDefinition("RWY CLSD", 1)));
    }

    [Fact]
    public void OnlineCondition_IsOneLineWithWhitespaceCollapsed()
    {
        var online = new StaticDefinition("A", 1)
        {
            OnlineCondition = "EPWA_APP,   EPWW_*_CTR\n epwa_f_app",
        };
        Assert.Equal(["Controllers: EPWA_APP, EPWW_*_CTR epwa_f_app"], StaticDefinitionSummary.Describe(online));

        var offline = new StaticDefinition("A", 1)
        {
            OnlineCondition = "NOT EPWA_DEL",
        };
        Assert.Equal(["Controllers: NOT EPWA_DEL"], StaticDefinitionSummary.Describe(offline));
    }

    [Theory]
    [InlineData("0600z-2246z", "Time: 0600z-2246z")]
    [InlineData("NOT 2200z-0600z", "Time: NOT 2200z-0600z")]
    [InlineData("LAX_TWR AND 2200z-0600z", "Controllers and time: LAX_TWR AND 2200z-0600z")]
    [InlineData("(LAX_TWR OR LAX_GND) 0600z-1400z", "Controllers and time: (LAX_TWR OR LAX_GND) 0600z-1400z")]
    [InlineData("LAX_TWR", "Controllers: LAX_TWR")]
    [InlineData("LAX_TWR AND (", "Controllers: LAX_TWR AND (")]
    public void OnlineLabel_ReflectsWhatTheConditionChecks(string condition, string expected)
    {
        var def = new StaticDefinition("A", 1) { OnlineCondition = condition };
        Assert.Equal([expected], StaticDefinitionSummary.Describe(def));
    }

    [Fact]
    public void Weather_IsOneLineWithWhitespaceCollapsed()
    {
        var def = new StaticDefinition("LVP", 1) { WeatherCondition = "  ceiling < 200ft\n  AND (rvr < 550m   OR vis < 100m) " };
        Assert.Equal(["Weather: ceiling < 200ft AND (rvr < 550m OR vis < 100m)"], StaticDefinitionSummary.Describe(def));
    }

    [Fact]
    public void BothConditions_GiveTwoLines_ControllersFirst()
    {
        var def = new StaticDefinition("X", 1)
        {
            OnlineCondition = "EPWA_APP",
            WeatherCondition = "cavok",
        };
        Assert.Equal(["Controllers: EPWA_APP", "Weather: cavok"], StaticDefinitionSummary.Describe(def));
    }

    [Fact]
    public void ACondition_WithNothingToCheck_IsIgnored()
    {
        var def = new StaticDefinition("X", 1)
        {
            OnlineCondition = "  ",
            WeatherCondition = " ",
        };
        Assert.Empty(StaticDefinitionSummary.Describe(def));
    }
}
