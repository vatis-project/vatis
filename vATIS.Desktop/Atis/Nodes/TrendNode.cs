// <copyright file="TrendNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder;
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
        if (Station == null || metar.TrendForecast == null)
            return;

        var voiceAtis = new List<string>();
        var textAtis = new List<string>();

        var decodedTrend = new DecodedMetar();
        ProcessTrendForecast(metar.TrendForecast, decodedTrend, voiceAtis, textAtis);

        if (metar.TrendForecastFuture != null)
        {
            var futureDecodedTrend = new DecodedMetar();
            ProcessTrendForecast(metar.TrendForecastFuture, futureDecodedTrend, voiceAtis, textAtis, isFuture: true);
        }

        VoiceAtis = string.Join(" ", voiceAtis);
        TextAtis = string.Join(" ", textAtis);
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

    private static void GetChunkResult(Dictionary<string, object> chunkResult, DecodedMetar decodedMetar)
    {
        if (chunkResult.TryGetValue("Result", out var value) && value is Dictionary<string, object>)
        {
            if (value is Dictionary<string, object> result)
            {
                foreach (var obj in result)
                {
                    typeof(DecodedMetar).GetProperty(obj.Key)?.SetValue(decodedMetar, obj.Value, null);
                }
            }
        }
    }

    private void ProcessTrendForecast(TrendForecast? forecast, DecodedMetar decodedTrend, List<string> voiceAtis,
        List<string> textAtis, bool isFuture = false)
    {
        if (forecast == null || Station == null)
            return;

        if (!isFuture)
        {
            voiceAtis.Add("TREND,");
        }

        switch (forecast.ChangeIndicator)
        {
            case TrendForecastType.Becoming:
                voiceAtis.Add("BECOMING");
                textAtis.Add("BECMG");
                break;
            case TrendForecastType.Temporary:
                voiceAtis.Add("TEMPORARY");
                textAtis.Add("TEMPO");
                break;
            case TrendForecastType.NoSignificantChanges:
                voiceAtis.Add("NO SIGNIFICANT CHANGES");
                textAtis.Add("NOSIG");
                break;
        }

        if (forecast.AtTime != null)
        {
            if (int.TryParse(forecast.AtTime, out var time))
            {
                voiceAtis.Add($"AT {time.ToSerialFormat()}.");
            }

            textAtis.Add($"AT{forecast.AtTime}");
        }

        if (forecast.FromTime != null)
        {
            if (int.TryParse(forecast.FromTime, out var time))
            {
                voiceAtis.Add($"FROM {time.ToSerialFormat()}.");
            }

            textAtis.Add($"FM{forecast.FromTime}");
        }

        if (forecast.UntilTime != null)
        {
            if (int.TryParse(forecast.UntilTime, out var time))
            {
                voiceAtis.Add($"UNTIL {time.ToSerialFormat()}.");
            }

            textAtis.Add($"TL{forecast.UntilTime}");
        }

        if (forecast.SurfaceWind != null)
        {
            var chunkResult = new SurfaceWindChunkDecoder().Parse(forecast.SurfaceWind);
            GetChunkResult(chunkResult, decodedTrend);
        }

        if (forecast.PrevailingVisibility != null)
        {
            if (forecast.PrevailingVisibility.Trim() == "CAVOK")
            {
                decodedTrend.Cavok = true;
            }
            else
            {
                var chunk = new VisibilityChunkDecoder();
                var chunkResult = chunk.Parse(forecast.PrevailingVisibility);
                GetChunkResult(chunkResult, decodedTrend);
            }
        }

        if (forecast.WeatherCodes != null)
        {
            var chunk = new PresentWeatherChunkDecoder();
            var chunkResult = chunk.Parse(forecast.WeatherCodes);
            GetChunkResult(chunkResult, decodedTrend);
        }

        if (forecast.Clouds?.Length > 0)
        {
            var chunk = new CloudChunkDecoder();
            var chunkResult = chunk.Parse(forecast.Clouds);
            GetChunkResult(chunkResult, decodedTrend);
        }

        if (decodedTrend.SurfaceWind != null)
        {
            var node = NodeParser.Parse<SurfaceWindNode, SurfaceWind>(decodedTrend, Station);
            voiceAtis.Add(node.VoiceAtis);
            textAtis.Add(node.TextAtis);
        }

        if (decodedTrend.Cavok)
        {
            voiceAtis.Add("CAV-OK.");
            textAtis.Add("CAVOK");
        }
        else if (decodedTrend.Visibility != null)
        {
            var node = NodeParser.Parse<PrevailingVisibilityNode, Visibility>(decodedTrend, Station);
            voiceAtis.Add(node.VoiceAtis);
            textAtis.Add(node.TextAtis);
        }

        if (decodedTrend.PresentWeather.Count > 0)
        {
            var node = NodeParser.Parse<PresentWeatherNode, WeatherPhenomenon>(decodedTrend, Station);
            voiceAtis.Add(node.VoiceAtis);
            textAtis.Add(node.TextAtis);
        }

        if (decodedTrend.Clouds.Count > 0)
        {
            var node = NodeParser.Parse<CloudNode, CloudLayer>(decodedTrend, Station);
            voiceAtis.Add(node.VoiceAtis);
            textAtis.Add(node.TextAtis);
        }
    }
}
