// <copyright file="AtisBuilderNotamTests.cs" company="Justin Shannon">
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

public class AtisBuilderNotamTests
{
    private const string RawMetar = "KVNY 031856Z 28012KT 10SM FEW015 18/11 A3002";

    private static async Task<(string Text, string Voice)> Build(string condition)
    {
        var builder = new AtisBuilder(null!, new FakeNavData(), null!, new FakeMetarRepository(), null!, null!);
        var station = new AtisStation { Identifier = "KVNY", Name = "KVNY", AtisType = AtisType.Combined };
        station.AtisVoice.UseTextToSpeech = false;
        station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement = false;
        station.NotamDefinitions.Add(new StaticDefinition("CHECK DENSITY ALTITUDE", 1, true)
        {
            WeatherCondition = condition,
        });

        var preset = new AtisPreset { Name = "P", Template = "[WIND] [NOTAMS]" };
        var metar = new MetarDecoder().ParseNotStrict(RawMetar);

        var text = await builder.BuildTextAtis(station, preset, 'P', metar, CancellationToken.None);
        var voice = await builder.BuildVoiceAtis(station, preset, 'P', metar, CancellationToken.None);
        return (text ?? string.Empty, voice.SpokenText ?? string.Empty);
    }

    [Fact]
    public async Task UnmetConditionalNotam_OmitsNotamsHeader()
    {
        var (text, voice) = await Build("temperature >= 29c");

        Assert.DoesNotContain("NOTAMS", text);
        Assert.DoesNotContain("NOTICES TO AIRMEN", voice.ToUpperInvariant());
    }

    [Fact]
    public async Task MetConditionalNotam_IncludesNotamsHeader()
    {
        var (text, voice) = await Build("temperature >= 10c");

        Assert.Contains("NOTAMS... CHECK DENSITY ALTITUDE", text);
        Assert.Contains("CHECK DENSITY ALTITUDE", voice);
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
