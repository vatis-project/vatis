// <copyright file="TrendChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System;
using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class TrendChunkDecoder : MetarChunkDecoder
{
    public override string GetRegex()
    {
        return @"(TREND|NOSIG|BECMG|TEMPO)\s*(?:(FM(\d{4}))?\s*(TL(\d{4}))?\s*(AT(\d{4}))?)?\s*([\w\d\/\s]+)?=";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        if (found.Count > 1)
        {
            var trend = new TrendForecast
            {
                ChangeIndicator = found[1].Value switch
                {
                    "NOSIG" => TrendForecastType.NoSignificantChanges,
                    "BECMG" => TrendForecastType.Becoming,
                    "TEMPO" => TrendForecastType.Temporary,
                    _ => throw new ArgumentException("Invalid ChangeIndicator")
                }
            };

            if (!string.IsNullOrEmpty(found[2].Value) && found[2].Value.StartsWith("FM"))
            {
                trend.FromTime = found[3].Value;
            }

            if (!string.IsNullOrEmpty(found[4].Value) && found[4].Value.StartsWith("TL"))
            {
                trend.UntilTime = found[5].Value;
            }

            if (!string.IsNullOrEmpty(found[6].Value) && found[6].Value.StartsWith("AT"))
            {
                trend.AtTime = found[7].Value;
            }

            if (!string.IsNullOrEmpty(found[8].Value))
            {
                trend.Forecast = found[8].Value;
            }
            
            result.Add(newRemainingMetar, trend);
        }

        return GetResults(newRemainingMetar, result);
    }
}