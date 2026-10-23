// <copyright file="AtisBuilderBlankLineTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class AtisBuilderBlankLineTests
{
    private const string RawMetar = "KSFO 031856Z 28012KT 10SM FEW015 18/11 A3002";

    private static async Task<string?> BuildText(AtisStation station, AtisPreset preset)
    {
        var builder = new AtisBuilder(null!, new FakeNavData(), null!, new FakeMetarRepository(), null!, null!);
        var metar = new MetarDecoder().ParseNotStrict(RawMetar);

        return await builder.BuildTextAtis(station, preset, 'P', metar, CancellationToken.None);
    }

    private static AtisStation NewStation()
    {
        return new AtisStation { Identifier = "KSFO", Name = "KSFO", AtisType = AtisType.Combined };
    }

    [Fact]
    public async Task TypedBlankLines_ArePreserved()
    {
        var preset = new AtisPreset
        {
            Name = "P",
            Template = "[ARPT_COND]|[NOTAMS]",
            AirportConditions = "A.\n\nB.",
            Notams = "X.\n\nY.",
        };

        var result = await BuildText(NewStation(), preset);

        Assert.StartsWith("A.\n\nB.|", result);
        Assert.Contains("X.\n\nY.", result);
    }

    [Fact]
    public async Task TrailingLineBreaks_AreTrimmedFromTheLastItem()
    {
        var station = NewStation();
        station.AirportConditionDefinitions.Add(new StaticDefinition("ONE\n", 1));
        var preset = new AtisPreset { Name = "P", Template = "[ARPT_COND]|", AirportConditions = "A.\n\nB.\n" };

        // Free text comes first, so its trailing line break stays and the static text's goes.
        var result = await BuildText(station, preset);
        Assert.StartsWith("A.\n\nB.\n ONE|", result);

        // Static text comes first, so its trailing line break stays and the free text's goes.
        station.AirportConditionsBeforeFreeText = true;
        result = await BuildText(station, preset);
        Assert.StartsWith("ONE\n A.\n\nB.|", result);
    }

    [Fact]
    public async Task StaticBlankLine_SeparatesDefinitions()
    {
        var station = NewStation();
        station.NotamDefinitions.Add(new StaticDefinition("ONE", 1));
        station.NotamDefinitions.Add(new StaticDefinition(string.Empty, 2));
        station.NotamDefinitions.Add(new StaticDefinition("TWO", 3));
        station.AirportConditionDefinitions.Add(new StaticDefinition("ONE", 1));
        station.AirportConditionDefinitions.Add(new StaticDefinition(string.Empty, 2));
        station.AirportConditionDefinitions.Add(new StaticDefinition("TWO", 3));
        var preset = new AtisPreset { Name = "P", Template = "[ARPT_COND]|[NOTAMS]" };

        var result = await BuildText(station, preset);

        Assert.StartsWith("ONE.\n\nTWO|", result);
        Assert.Contains("ONE.\n\nTWO", result![8..]);
    }

    private sealed class FakeNavData : INavDataRepository
    {
        public Task Initialize() => Task.CompletedTask;

        public Task CheckForUpdates() => Task.CompletedTask;

        public Airport? GetAirport(string id) => new() { Id = id, Name = id };

        public Navaid? GetNavaid(string id) => null;
    }

    private sealed class FakeMetarRepository : IMetarRepository
    {
        public Task<DecodedMetar?> GetMetar(string station, bool monitor = false, bool triggerMessageBus = true,
            string? customUrl = null) => Task.FromResult<DecodedMetar?>(null);

        public void RemoveMetar(string station)
        {
        }
    }
}
