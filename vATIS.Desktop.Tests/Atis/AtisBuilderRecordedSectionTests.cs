// <copyright file="AtisBuilderRecordedSectionTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.TextToSpeech;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

public class AtisBuilderRecordedSectionTests
{
    private const string RawMetar = "KVNY 031856Z 28012KT 10SM FEW015 18/11 A3002";
    private const int FrameBytes = 960 * 2;

    private static (AtisBuilder Builder, FakeTextToSpeech Tts, AtisStation Station, AtisPreset Preset, DecodedMetar Metar)
        Create()
    {
        var tts = new FakeTextToSpeech();
        var builder = new AtisBuilder(null!, new FakeNavData(), tts, new FakeMetarRepository(), null!, null!);
        var station = new AtisStation { Identifier = "KVNY", Name = "KVNY", AtisType = AtisType.Combined };
        station.AtisVoice.UseTextToSpeech = true;
        station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement = false;

        var preset = new AtisPreset
        {
            Name = "P",
            Template = "ALPHA [NOTAMS] BRAVO",
            Notams = "RUNWAY CLOSED",
        };
        return (builder, tts, station, preset, new MetarDecoder().ParseNotStrict(RawMetar));
    }

    [Fact]
    public async Task NoRecordings_SynthesizesWholeAtisAsBefore()
    {
        var (builder, tts, station, preset, metar) = Create();

        var result = await builder.BuildVoiceAtis(station, preset, 'P', metar, CancellationToken.None);

        Assert.Single(tts.Requests);
        Assert.Contains("RUNWAY CLOSED", tts.Requests[0]);
        Assert.Equal(tts.Audio, result.AudioBytes);
    }

    [Fact]
    public async Task RecordedNotams_AreSplicedBetweenSynthesizedPieces()
    {
        var (builder, tts, station, preset, metar) = Create();
        var recording = Enumerable.Repeat((byte)7, 4000).ToArray();

        var result = await builder.BuildVoiceAtis(station, preset, 'P', metar, CancellationToken.None,
            recordings: new VoiceSectionRecordings(null, recording));

        Assert.Equal(2, tts.Requests.Count);
        Assert.Contains("ALPHA", tts.Requests[0]);
        Assert.Contains("BRAVO", tts.Requests[1]);
        Assert.DoesNotContain(tts.Requests, r => r.Contains("RUNWAY CLOSED") || r.Contains("ZZRECORDED"));

        var audio = result.AudioBytes!;
        Assert.Equal(0, audio.Length % FrameBytes);
        var start = tts.Audio.Length + 24000;
        Assert.Equal(recording, audio.Skip(start).Take(recording.Length).ToArray());
        Assert.Contains("[RECORDED]", result.SpokenText);
    }

    [Fact]
    public async Task RecordingForEmptySection_IsIgnored()
    {
        var (builder, tts, station, preset, metar) = Create();
        preset.Notams = string.Empty;

        var result = await builder.BuildVoiceAtis(station, preset, 'P', metar, CancellationToken.None,
            recordings: new VoiceSectionRecordings(null, [1, 2, 3]));

        Assert.Single(tts.Requests);
        Assert.Equal(tts.Audio, result.AudioBytes);
    }

    [Fact]
    public async Task VoiceSectionText_ReturnsSpokenNotamsAndEmptyConditions()
    {
        var (builder, _, station, preset, metar) = Create();

        var sections = await builder.BuildVoiceSectionText(station, preset, 'P', metar, CancellationToken.None);

        Assert.Contains("RUNWAY CLOSED", sections.Notams);
        Assert.Equal(string.Empty, sections.AirportConditions);
    }

    private sealed class FakeTextToSpeech : ITextToSpeechService
    {
        public List<string> Requests { get; } = [];

        public byte[] Audio { get; } = new byte[FrameBytes * 3];

        public List<VoiceMetaData> VoiceList { get; } = [];

        public IReadOnlyDictionary<string, string> BuiltInContractions { get; } = new Dictionary<string, string>();

        public Task Initialize() => Task.CompletedTask;

        public Task<byte[]?> RequestAudio(string text, AtisStation station, char atisLetter,
            CancellationToken cancellationToken)
        {
            Requests.Add(text);
            return Task.FromResult<byte[]?>(Audio);
        }
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
