// <copyright file="PresentWeatherNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides present weather information.
/// </summary>
public class PresentWeatherNode : BaseNode<WeatherPhenomenon>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.PresentWeather);
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

    private void Parse(List<WeatherPhenomenon> weathers)
    {
        ArgumentNullException.ThrowIfNull(Station);

        var voiceAtis = new List<string>();
        var textAtis = new List<string>();

        foreach (var weather in weathers)
        {
            voiceAtis.Add(FormatWeatherVoice(weather));
            textAtis.Add(FormatWeatherText(weather));
        }

        var voiceTemplate = Station.AtisFormat.PresentWeather.Template.Voice;
        var textTemplate = Station.AtisFormat.PresentWeather.Template.Text;

        if (voiceTemplate != null)
        {
            VoiceAtis = voiceAtis.Count > 0
                ? Regex.Replace(voiceTemplate, "{weather}", string.Join(", ", voiceAtis), RegexOptions.IgnoreCase)
                : string.Empty;
        }

        if (textTemplate != null)
        {
            TextAtis = textAtis.Count > 0
                ? Regex.Replace(textTemplate, "{weather}", string.Join(" ", textAtis), RegexOptions.IgnoreCase)
                : string.Empty;
        }
    }

    private string FormatWeatherVoice(WeatherPhenomenon? weather)
    {
        if (weather == null)
            return string.Empty;

        ArgumentNullException.ThrowIfNull(Station);

        var result = new List<string>();

        if (weather.RawValue != null &&
            Station.AtisFormat.PresentWeather.PresentWeatherTypes.TryGetValue(weather.RawValue.Trim(), out var value))
        {
            result.Add(value.Spoken.Trim());
        }

        return result.Count > 0 ? string.Join(" ", result) : string.Empty;
    }

    private string FormatWeatherText(WeatherPhenomenon? weather)
    {
        if (weather == null)
            return string.Empty;

        ArgumentNullException.ThrowIfNull(Station);

        var result = new List<string>();

        if (weather.RawValue != null &&
            Station.AtisFormat.PresentWeather.PresentWeatherTypes.TryGetValue(weather.RawValue.Trim(), out var value))
        {
            result.Add(value.Text.Trim());
        }

        return result.Count > 0 ? string.Join(" ", result) : string.Empty;
    }
}
