// <copyright file="AtisBuilderFormatCustomizationTests.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Atis;
using Vatsim.Vatis.NavData;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Xunit;

namespace Vatsim.Vatis.Tests.Atis;

/// <summary>
/// Builds complete ATIS messages through <see cref="AtisBuilder"/> to verify that customized
/// text and voice formats flow through every component independently.
/// </summary>
public class AtisBuilderFormatCustomizationTests
{
    private const string RawMetar =
        "VVTS 011650Z 25009G15KT 0800 R25L/0600U +RA FEW015 BKN030CB 25/24 Q1015";

    private const string Template = "[WIND] | [RVR] | [VIS] | [PRESENT_WX] | [CLOUDS] | [TEMP]/[DEW] | [PRESSURE]";

    private static AtisStation NewStation()
    {
        var station = new AtisStation { Identifier = "VVTS", Name = "VVTS", AtisType = AtisType.Combined };
        station.AtisVoice.UseTextToSpeech = false;
        station.AtisFormat.ClosingStatement.AutoIncludeClosingStatement = false;
        return station;
    }

    private static async Task<(string Text, string Voice)> Build(Action<AtisStation>? customize = null)
    {
        var station = NewStation();
        customize?.Invoke(station);

        var builder = new AtisBuilder(null!, new FakeNavData(), null!, new FakeMetarRepository(), null!, null!);
        var preset = new AtisPreset { Name = "P", Template = Template };
        var metar = new MetarDecoder().ParseNotStrict(RawMetar);

        var text = await builder.BuildTextAtis(station, preset, 'P', metar, CancellationToken.None);
        var voice = await builder.BuildVoiceAtis(station, preset, 'P', metar, CancellationToken.None);

        return (text ?? string.Empty, voice.SpokenText ?? string.Empty);
    }

    [Fact]
    public async Task DefaultFormats_ProduceExpectedOutput()
    {
        var (text, voice) = await Build();

        Assert.Contains("25009G15KT", text);
        Assert.Contains("R25L/0600U", text);
        Assert.Contains("BKN030CB", text);
        Assert.Contains("RUNWAY TWO FIVE LEFT", voice);
        Assert.Contains("GOING UP", voice);
        Assert.Contains("CUMULONIMBUS", voice);
    }

    [Fact]
    public async Task CustomFormats_ApplyToTextAndVoiceIndependently()
    {
        var (defaultText, defaultVoice) = await Build();

        var (text, voice) = await Build(station =>
        {
            var format = station.AtisFormat;

            // Variables.
            format.Clouds.AltitudeFormat.Text.Suffix = "FT";
            format.Clouds.AltitudeFormat.Voice.Suffix = " feet";

            // Text-only counterparts of voice fields.
            format.Clouds.ConvectiveTypesText["CB"] = "CBX";
            format.RunwayVisualRange.TendencyText["U"] = "UP";

            // Voice-only customizations.
            format.RunwayVisualRange.GoingUpTendency = "rising";
        });

        // Text reflects the text customizations only.
        Assert.Contains("CBX", text);
        Assert.Contains("R25L/0600UP", text);
        Assert.Contains("FEW015FT", text);
        Assert.DoesNotContain("RISING", text);

        // Voice reflects the voice customizations only.
        Assert.Contains("RISING", voice);
        Assert.DoesNotContain("CBX", voice);
        Assert.Contains("ONE THOUSAND FIVE HUNDRED FEET", voice);
        Assert.DoesNotContain("FEET", text);

        // The un-customized side of each pair is unchanged.
        Assert.Contains("GOING UP", defaultVoice);
        Assert.DoesNotContain("GOING UP", voice);
        Assert.Contains("R25L/0600U", defaultText);
    }

    [Fact]
    public async Task CustomWeatherDescriptors_ApplyToTextAndVoiceIndependently()
    {
        var (text, voice) = await Build(station =>
        {
            var wx = station.AtisFormat.PresentWeather;
            wx.PresentWeatherTypes["+RA"] = new Vatsim.Vatis.Profiles.AtisFormat.Nodes.PresentWeather.WeatherDescriptorType("HVY RAIN", "severe downpour");
        });

        Assert.Contains("HVY RAIN", text);
        Assert.DoesNotContain("SEVERE", text);
        Assert.Contains("SEVERE DOWNPOUR", voice);
        Assert.DoesNotContain("HVY", voice);
    }

    [Fact]
    public async Task CustomWindFormats_ApplyToTextAndVoiceIndependently()
    {
        var (text, voice) = await Build(station =>
        {
            var wind = station.AtisFormat.SurfaceWind;
            wind.VariableFormats["wind_spd"].Voice.Suffix = " knots";
            wind.VariableFormats["wind_spd"].Text.Suffix = "/";
        });

        Assert.Contains("09/G15KT", text);
        Assert.DoesNotContain("KNOTS", text);
        Assert.Contains("NINER KNOTS", voice);
        Assert.DoesNotContain("/", voice.Split('|')[0]);
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
