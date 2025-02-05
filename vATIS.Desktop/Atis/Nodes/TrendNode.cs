// <copyright file="TrendNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides trend information.
/// </summary>
public class TrendNode : BaseNode<TrendForecast>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        if (metar.TrendForecast == null)
            return;

        if (Station == null)
            return;

        var tts = new List<string>();
        var acars = new List<string>();

        switch (metar.TrendForecast.ChangeIndicator)
        {
            case TrendForecastType.Becoming:
                tts.Add("TREND, BECOMING");
                acars.Add("BECMG");
                break;
            case TrendForecastType.Temporary:
                tts.Add("TREND, TEMPORARY");
                acars.Add("TEMPO");
                break;
            case TrendForecastType.NoSignificantChanges:
                tts.Add("TREND, NO SIGNIFICANT CHANGES");
                acars.Add("NOSIG");
                break;
        }

        if (metar.TrendForecast.FromTime != null)
        {
            if (int.TryParse(metar.TrendForecast.FromTime, out var time))
            {
                tts.Add($"FROM {time.ToSerialFormat()}");
            }

            acars.Add("FM" + metar.TrendForecast.FromTime);
        }

        if (metar.TrendForecast.UntilTime != null)
        {
            if (int.TryParse(metar.TrendForecast.UntilTime, out var time))
            {
                tts.Add($"UNTIL {time.ToSerialFormat()}");
            }

            acars.Add("TL" + metar.TrendForecast.UntilTime);
        }

        if (metar.TrendForecast.AtTime != null)
        {
            if (int.TryParse(metar.TrendForecast.AtTime, out var time))
            {
                tts.Add($"AT {time.ToSerialFormat()}");
            }

            acars.Add("AT" + metar.TrendForecast.AtTime);
        }

        var decoder = new MetarDecoder();

        if (metar.TrendForecast.Forecast != null)
        {
            var forecast = decoder.ParseNotStrict(metar.TrendForecast.Forecast);

            if (forecast.SurfaceWind != null)
            {
                var surfaceWind = NodeParser.Parse<SurfaceWindNode, SurfaceWind>(forecast, Station);
                tts.Add(surfaceWind.VoiceAtis);
                acars.Add(surfaceWind.TextAtis);
            }

            if (forecast.Visibility != null)
            {
                var visibility = NodeParser.Parse<PrevailingVisibilityNode, Visibility>(forecast, Station);
                tts.Add(visibility.VoiceAtis);
                acars.Add(visibility.TextAtis);
            }

            if (forecast.PresentWeather.Count > 0)
            {
                var presentWeather = NodeParser.Parse<PresentWeatherNode, WeatherPhenomenon>(forecast, Station);
                tts.Add(presentWeather.VoiceAtis);
                acars.Add(presentWeather.TextAtis);
            }

            if (forecast.Clouds.Count > 0)
            {
                var clouds = NodeParser.Parse<CloudNode, CloudLayer>(forecast, Station);
                tts.Add(clouds.VoiceAtis);
                acars.Add(clouds.TextAtis);
            }
        }

        if (metar.TrendForecastAdditional?.Forecast != null)
        {
            var forecast = decoder.ParseNotStrict(metar.TrendForecastAdditional.Forecast);

            switch (metar.TrendForecastAdditional.ChangeIndicator)
            {
                case TrendForecastType.Becoming:
                    tts.Add("BECOMING");
                    acars.Add("BECMG");
                    break;
                case TrendForecastType.Temporary:
                    tts.Add("TEMPORARY");
                    acars.Add("TEMPO");
                    break;
                case TrendForecastType.NoSignificantChanges:
                    tts.Add("NO SIGNIFICANT CHANGES");
                    acars.Add("NOSIG");
                    break;
            }

            if (metar.TrendForecastAdditional.FromTime != null)
            {
                if (int.TryParse(metar.TrendForecastAdditional.FromTime, out var time))
                {
                    tts.Add($"FROM {time.ToSerialFormat()}");
                }

                acars.Add("FM" + metar.TrendForecastAdditional.FromTime);
            }

            if (metar.TrendForecastAdditional.UntilTime != null)
            {
                if (int.TryParse(metar.TrendForecastAdditional.UntilTime, out var time))
                {
                    tts.Add($"UNTIL {time.ToSerialFormat()}");
                }

                acars.Add("TL" + metar.TrendForecastAdditional.UntilTime);
            }

            if (metar.TrendForecastAdditional.AtTime != null)
            {
                if (int.TryParse(metar.TrendForecastAdditional.AtTime, out var time))
                {
                    tts.Add($"AT {time.ToSerialFormat()}");
                }

                acars.Add("AT" + metar.TrendForecastAdditional.AtTime);
            }

            if (forecast.SurfaceWind != null)
            {
                var surfaceWind = NodeParser.Parse<SurfaceWindNode, SurfaceWind>(forecast, Station);
                tts.Add(surfaceWind.VoiceAtis);
                acars.Add(surfaceWind.TextAtis);
            }

            if (forecast.Visibility != null)
            {
                var visibility = NodeParser.Parse<PrevailingVisibilityNode, Visibility>(forecast, Station);
                tts.Add(visibility.VoiceAtis);
                acars.Add(visibility.TextAtis);
            }

            if (forecast.PresentWeather.Count > 0)
            {
                var presentWeather = NodeParser.Parse<PresentWeatherNode, WeatherPhenomenon>(forecast, Station);
                tts.Add(presentWeather.VoiceAtis);
                acars.Add(presentWeather.TextAtis);
            }

            if (forecast.Clouds.Count > 0)
            {
                var clouds = NodeParser.Parse<CloudNode, CloudLayer>(forecast, Station);
                tts.Add(clouds.VoiceAtis);
                acars.Add(clouds.TextAtis);
            }
        }

        VoiceAtis = string.Join(" ", tts);
        TextAtis = string.Join(" ", acars);
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(TrendForecast value, string? format)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(TrendForecast node, string? format)
    {
        throw new NotImplementedException();
    }
}
