// <copyright file="AtisBuilderTransitionLevelTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
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

public class AtisBuilderTransitionLevelTests
{
    private const string OwnMetar = "EPWA 031830Z 27010KT 9999 FEW030 10/05 Q1010";

    private static readonly Dictionary<string, string> Metars = new()
    {
        ["EPKK"] = "EPKK 031830Z 27010KT 9999 FEW030 10/05 Q0993",
        ["EPGD"] = "EPGD 031830Z 27010KT 9999 FEW030 10/05 Q1005",
    };

    private static async Task<string?> BuildTl(params string[] airports)
    {
        var builder = new AtisBuilder(null!, new FakeNavData(), null!, new FakeMetarRepository(), null!, null!);
        var station = new AtisStation { Identifier = "EPWA", Name = "EPWA", AtisType = AtisType.Combined };
        station.AtisFormat.TransitionLevel.Values =
        [
            new TransitionLevelMeta(0, 994, 90),
            new TransitionLevelMeta(995, 9999, 80),
        ];
        station.AtisFormat.TransitionLevel.AirportIcaos = [..airports];

        var preset = new AtisPreset { Name = "P", Template = "[TL]" };
        var metar = new MetarDecoder().ParseNotStrict(OwnMetar);

        return await builder.BuildTextAtis(station, preset, 'P', metar, CancellationToken.None);
    }

    [Fact]
    public async Task NoAirportList_UsesOwnQnh()
    {
        Assert.StartsWith("TRANSITION LEVEL 80", await BuildTl());
    }

    [Fact]
    public async Task ListedAirportBelowThreshold_LowersToHigherTransitionLevel()
    {
        Assert.StartsWith("TRANSITION LEVEL 90", await BuildTl("EPKK", "EPGD"));
    }

    [Fact]
    public async Task ListedAirportsAllAboveThreshold_KeepsLowerTransitionLevel()
    {
        Assert.StartsWith("TRANSITION LEVEL 80", await BuildTl("EPGD"));
    }

    [Fact]
    public async Task ListedAirportWithoutMetar_IsIgnored()
    {
        Assert.StartsWith("TRANSITION LEVEL 80", await BuildTl("EPXX"));
    }

    [Fact]
    public async Task ListedAirportFetchThrows_IsIgnored()
    {
        Assert.StartsWith("TRANSITION LEVEL 80", await BuildTl("FAIL"));
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
            string? customUrl = null)
        {
            if (station == "FAIL")
            {
                throw new InvalidOperationException("boom");
            }

            return Task.FromResult(Metars.TryGetValue(station, out var raw)
                ? new MetarDecoder().ParseNotStrict(raw)
                : null);
        }

        public void RemoveMetar(string station)
        {
        }
    }
}
