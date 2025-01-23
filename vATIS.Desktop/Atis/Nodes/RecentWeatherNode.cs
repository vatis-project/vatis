// <copyright file="RecentWeatherNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides recent weather information.
/// </summary>
public class RecentWeatherNode : BaseNode<WeatherPhenomenon>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        if (metar.RecentWeather != null)
        {
            Parse(metar.RecentWeather);
        }
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(WeatherPhenomenon value, string? format)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(WeatherPhenomenon node, string? format)
    {
        throw new NotImplementedException();
    }

    private void Parse(WeatherPhenomenon weather)
    {
        ArgumentNullException.ThrowIfNull(Station);

        var voice = FormatWeatherVoice(weather);
        var text = FormatWeatherText(weather);

        var voiceTemplate = Station.AtisFormat.RecentWeather.Template.Voice;
        var textTemplate = Station.AtisFormat.RecentWeather.Template.Text;

        if (voiceTemplate != null)
        {
            VoiceAtis = !string.IsNullOrEmpty(voice)
                ? Regex.Replace(voiceTemplate, "{weather}", string.Join(", ", voice).Trim(',').Trim(' '),
                    RegexOptions.IgnoreCase)
                : string.Empty;
        }

        if (textTemplate != null)
        {
            TextAtis = !string.IsNullOrEmpty(voice)
                ? Regex.Replace(textTemplate, "{weather}", string.Join(" ", text).Trim(' '),
                    RegexOptions.IgnoreCase)
                : string.Empty;
        }
    }

    private string FormatWeatherVoice(WeatherPhenomenon? weather)
    {
        if (weather == null)
            return string.Empty;

        ArgumentNullException.ThrowIfNull(Station);

        var result = new List<string>();

        switch (weather.IntensityProximity)
        {
            case "-":
                result.Add(Station.AtisFormat.PresentWeather.LightIntensity.Voice ?? "");
                break;
            case "+":
                result.Add(Station.AtisFormat.PresentWeather.HeavyIntensity.Voice ?? "");
                break;
            default:
                result.Add(Station.AtisFormat.PresentWeather.ModerateIntensity.Voice ?? "");
                break;
        }

        if (!string.IsNullOrEmpty(weather.Characteristics))
            result.Add(Station.AtisFormat.PresentWeather.PresentWeatherTypes[weather.Characteristics].Spoken);

        result.AddRange(weather.Types.Select(type => Station.AtisFormat.PresentWeather.PresentWeatherTypes[type].Spoken));

        if (weather.IntensityProximity == "VC")
            result.Add(Station.AtisFormat.PresentWeather.Vicinity.Voice ?? "");

        return string.Join(" ", result);
    }

    private string FormatWeatherText(WeatherPhenomenon? weather)
    {
        if (weather == null)
            return string.Empty;

        ArgumentNullException.ThrowIfNull(Station);

        var result = new List<string>();

        switch (weather.IntensityProximity)
        {
            case "-":
                result.Add(Station.AtisFormat.PresentWeather.LightIntensity.Text ?? "");
                break;
            case "+":
                result.Add(Station.AtisFormat.PresentWeather.HeavyIntensity.Text ?? "");
                break;
            default:
                result.Add(Station.AtisFormat.PresentWeather.ModerateIntensity.Text ?? "");
                break;
        }

        if (!string.IsNullOrEmpty(weather.Characteristics))
            result.Add(Station.AtisFormat.PresentWeather.PresentWeatherTypes[weather.Characteristics].Text);

        result.AddRange(weather.Types.Select(type => Station.AtisFormat.PresentWeather.PresentWeatherTypes[type].Text));

        if (weather.IntensityProximity == "VC")
            result.Add(Station.AtisFormat.PresentWeather.Vicinity.Text ?? "");

        return string.Join("", result);
    }
}
