// <copyright file="AtisBuilderMetarVariableTests.cs" company="Justin Shannon">
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

public class AtisBuilderMetarVariableTests
{
    private const string RawMetar = "FALA 241100Z 31005KT 210V010 CAVOK 29/M00 Q1020 NOSIG";

    private static async Task<string?> BuildText(string template)
    {
        var builder = new AtisBuilder(null!, new FakeNavData(), null!, new FakeMetarRepository(), null!);
        var station = new AtisStation { Identifier = "FALA", Name = "FALA", AtisType = AtisType.Combined };
        var preset = new AtisPreset { Name = "P", Template = template };
        var metar = new MetarDecoder().ParseNotStrict(RawMetar);

        return await builder.BuildTextAtis(station, preset, 'P', metar, CancellationToken.None);
    }

    [Theory]
    [InlineData("METAR [METAR] ADZ")]
    [InlineData("METAR $METAR ADZ")]
    public async Task MetarVariable_InsertsRawMetar(string template)
    {
        var result = await BuildText(template);

        // A closing statement may be appended automatically, so only the start is compared.
        Assert.StartsWith($"METAR {RawMetar} ADZ", result);
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
