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

/// <summary>
/// Represents a decoder for parsing METAR trend information chunks such as "NOSIG", "BECMG", or "TEMPO".
/// </summary>
/// <remarks>
/// This class is responsible for recognizing and extracting trend-related data from METAR reports.
/// It extends the <see cref="MetarChunkDecoder"/> abstract class to provide specific implementation for decoding trend chunks.
/// </remarks>
public sealed class TrendChunkDecoder : MetarChunkDecoder
{
    /// <inheritdoc/>
    public override string GetRegex()
    {
        return @"TREND (BECMG|TEMPO|NOSIG)\s*(FM\d{4})?\s*(AT\d{4})?\s*(TL\d{4})?\s*(\d{4}\s*\w+)\s*(?:((?:TEMPO|BECMG|NOSIG)\s*(?:FM\d{4}|TL\d{4}|AT\d{4})?\s*(?:\s*.+)))?";
    }

    /// <inheritdoc/>
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
                    _ => throw new ArgumentException("Invalid ChangeIndicator"),
                },
            };

            if (!string.IsNullOrEmpty(found[2].Value))
            {
                trend.FromTime = found[2].Value;
            }

            if (!string.IsNullOrEmpty(found[3].Value))
            {
                trend.UntilTime = found[3].Value;
            }

            if (!string.IsNullOrEmpty(found[4].Value))
            {
                trend.AtTime = found[4].Value;
            }

            // Prefix the forecasts with a fake airport ID for later parsing with the METAR decoder.
            if (!string.IsNullOrEmpty(found[5].Value))
            {
                trend.Forecast = $"ZZZZ {found[5].Value.Trim()}";
            }

            if (!string.IsNullOrEmpty(found[6].Value))
            {
                trend.SecondForecast = $"ZZZZ {found[6].Value.Trim()}";
            }

            result.Add("TrendForecast", trend);
        }

        return GetResults(newRemainingMetar, result);
    }
}
