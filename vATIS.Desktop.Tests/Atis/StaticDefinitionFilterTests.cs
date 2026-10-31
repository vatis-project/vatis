// <copyright file="StaticDefinitionFilterTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Networking.Vatsim;
using Vatsim.Vatis.Profiles.Models;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class StaticDefinitionFilterTests
{
    private static StaticDefinition Def(string text, int ordinal, string? condition = null, bool enabled = true)
    {
        return new StaticDefinition(text, ordinal, enabled) { OnlineCondition = condition };
    }

    private static string[] Active(StaticDefinition[] defs, params string[] online)
    {
        return StaticDefinitionFilter.GetActive(defs, online).Select(x => x.Text).ToArray();
    }

    [Fact]
    public void Unconditional_AlwaysIncluded_OrderedAndEnabledOnly()
    {
        var defs = new[] { Def("B", 2), Def("A", 1), Def("C", 3, enabled: false) };
        Assert.Equal(["A", "B"], Active(defs));
    }

    [Fact]
    public void WhenOnline_RequiresMatchingCallsign()
    {
        var defs = new[] { Def("CLR VIA DATALINK", 1, "EPWA_DEL") };
        Assert.Equal(["CLR VIA DATALINK"], Active(defs, "EPWA_DEL"));
        Assert.Empty(Active(defs, "EPWA_APP"));
    }

    [Fact]
    public void Not_IsTrueWhileTheControllerIsOffline()
    {
        var defs = new[] { Def("CONTACT DELIVERY", 1, "NOT EPWA_DEL") };
        Assert.Empty(Active(defs, "EPWA_DEL"));
        Assert.Equal(["CONTACT DELIVERY"], Active(defs, "EPWA_APP"));
    }

    [Fact]
    public void Wildcards_MultipleCallsigns_AndCaseInsensitive()
    {
        var defs = new[] { Def("A", 1, "epwa_app, EPWW_*_CTR") };
        Assert.Equal(["A"], Active(defs, "EPWA_APP"));
        Assert.Equal(["A"], Active(defs, "EPWW_W_CTR"));
        Assert.Empty(Active(defs, "EPWA_TWR", "EPWW_CTR"));
    }

    [Fact]
    public void UnknownFeed_TreatedAsNobodyOnline()
    {
        var onlineDef = Def("A", 1, "EPWA_APP");
        var offlineDef = Def("B", 2, "NOT EPWA_APP");
        var active = StaticDefinitionFilter.GetActive([onlineDef, offlineDef], null);
        Assert.Equal(["B"], active.Select(x => x.Text).ToArray());
    }

    [Fact]
    public void ConditionWithoutCallsigns_IsIgnored()
    {
        var defs = new[] { Def("A", 1, "  ") };
        Assert.Equal(["A"], Active(defs));
        Assert.False(StaticDefinitionFilter.HasConditions(defs));
    }

    [Fact]
    public void HasConditions_OnlyCountsEnabledConditionalDefinitions()
    {
        Assert.True(StaticDefinitionFilter.HasConditions([Def("A", 1, "NOT X_APP")]));
        Assert.False(StaticDefinitionFilter.HasConditions(
            [Def("A", 1, "NOT X_APP", enabled: false)]));
    }

    [Fact]
    public void ParseControllers_ExcludesObserversButKeepsTowers()
    {
        const string json = """
            {"controllers":[
              {"callsign":"epwa_app","facility":5},
              {"callsign":"EPWA_DEL","facility":2},
              {"callsign":"EPWA_OBS","facility":0},
              {"callsign":"EPWA_TWR","facility":4},
              {"callsign":null,"facility":6}],
             "atis":[{"callsign":"EPWA_ATIS","facility":4}],
             "pilots":[]}
            """;
        var controllers = VatsimDataService.ParseControllers(json);
        Assert.Equal(3, controllers.Count);
        Assert.Contains("EPWA_APP", controllers);
        Assert.Contains("EPWA_DEL", controllers);
        Assert.Contains("EPWA_TWR", controllers);
    }

    [Fact]
    public void Forced_IncludesDefinitionWhoseConditionIsNotMet()
    {
        var conditional = Def("LVP IN PROGRESS", 1, "EPWA_DEL");
        var defs = new[] { conditional, Def("B", 2) };
        var forced = new HashSet<StaticDefinition> { conditional };

        var active = StaticDefinitionFilter.GetActive(defs, [], forced: forced).Select(x => x.Text);
        Assert.Equal(["LVP IN PROGRESS", "B"], active);
    }

    [Fact]
    public void Forced_DoesNotIncludeDisabledDefinition()
    {
        var disabled = Def("A", 1, "EPWA_DEL", enabled: false);
        var forced = new HashSet<StaticDefinition> { disabled };

        Assert.Empty(StaticDefinitionFilter.GetActive([disabled], [], forced: forced));
    }

    [Fact]
    public void Hidden_LeavesOutDefinitionEvenWhenItsConditionIsMet()
    {
        var a = Def("A", 1);
        var b = Def("B", 2, "EPWA_DEL");
        var hidden = new HashSet<StaticDefinition> { a, b };

        Assert.Empty(StaticDefinitionFilter.GetActive([a, b], ["EPWA_DEL"], hidden: hidden));
    }

    [Fact]
    public void OrdinalOverrides_ChangeTheOrder()
    {
        var a = Def("A", 1);
        var b = Def("B", 2);
        var overrides = new Dictionary<StaticDefinition, int> { [a] = 2, [b] = 1 };

        var active = StaticDefinitionFilter.GetActive([a, b], [], ordinalOverrides: overrides);
        Assert.Equal(["B", "A"], active.Select(x => x.Text));
    }

    [Fact]
    public void HasCondition_IsTrueForControllerOrWeatherConditions()
    {
        Assert.False(StaticDefinitionFilter.HasCondition(Def("A", 1)));
        Assert.True(StaticDefinitionFilter.HasCondition(Def("A", 1, "EPWA_DEL")));
        Assert.True(StaticDefinitionFilter.HasCondition(
            new StaticDefinition("A", 1) { WeatherCondition = "visibility < 800m" }));
    }

    [Fact]
    public void Clone_PreservesCondition()
    {
        var clone = Def("A", 1, "EPWA_APP").Clone();
        Assert.Equal("EPWA_APP", clone.OnlineCondition);
    }
}
