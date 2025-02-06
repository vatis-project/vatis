// <copyright file="TrendChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Surface Wind
    private const string WindDirectionRegexPattern = "(?:[0-9]{3}|VRB|///)";
    private const string WindSpeedRegexPattern = "P?(?:[/0-9]{2,3}|//)";
    private const string WindSpeedVariationsRegexPattern = "(?:GP?(?:[0-9]{2,3}))?";
    private const string WindUnitRegexPattern = "(?:KT|MPS|KPH)";

    // Prevailing Visibility
    private const string VisibilityRegexPattern = "(?:[0-9]{4})?";

    // Present Weather
    private const string CaracRegexPattern = "TS|FZ|SH|BL|DR|MI|BC|PR";
    private const string TypeRegexPattern = "DZ|RA|SN|SG|PL|DS|GR|GS|UP|IC|FG|BR|SA|DU|HZ|FU|VA|PY|DU|PO|SQ|FC|DS|SS|//";

    // Clouds
    private const string NoCloudRegexPattern = "(?:NSC|NCD|CLR|SKC)";
    private const string LayerRegexPattern = "(?:VV|FEW|SCT|BKN|OVC|///)(?:[0-9]{3}|///)(?:CB|TCU|///)?";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        var windRegex = $"{WindDirectionRegexPattern}{WindSpeedRegexPattern}{WindSpeedVariationsRegexPattern}{WindUnitRegexPattern}";
        var visibilityRegex = $"{VisibilityRegexPattern}|CAVOK";
        var presentWeatherRegex = $@"(?:[-+]|VC)?(?:{CaracRegexPattern})?(?:{TypeRegexPattern})?(?:{TypeRegexPattern})?(?:{TypeRegexPattern})?";
        var cloudRegex = $@"(?:{NoCloudRegexPattern}|(?:{LayerRegexPattern})(?: {LayerRegexPattern})?(?: {LayerRegexPattern})?(?: {LayerRegexPattern})?)";
        Debug.WriteLine(cloudRegex);
        return $@"TREND (TEMPO|BECMG|NOSIG)\s*(?:AT(\d{{4}}))?\s*(?:FM(\d{{4}}))?\s*(?:TL(\d{{4}}))?\s*({windRegex})?\s*({visibilityRegex})?\s*({presentWeatherRegex})?\s*({cloudRegex})?\s*((?=\s*(?:TEMPO|BECMG|NOSIG|$))(?:\s*(TEMPO|BECMG|NOSIG)\s*(?:AT(\d{{4}}))?\s*(?:FM(\d{{4}}))?\s*(?:TL(\d{{4}}))?\s*(.+))?)";
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
            var firstTrend = new TrendForecast
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
                firstTrend.AtTime = found[2].Value;
            }

            if (!string.IsNullOrEmpty(found[3].Value))
            {
                firstTrend.FromTime = found[3].Value;
            }

            if (!string.IsNullOrEmpty(found[4].Value))
            {
                firstTrend.UntilTime = found[4].Value;
            }

            if (!string.IsNullOrEmpty(found[5].Value))
            {
                firstTrend.SurfaceWind = found[5].Value + " ";
            }

            if (!string.IsNullOrEmpty(found[6].Value))
            {
                firstTrend.PrevailingVisibility = found[6].Value + " ";
            }

            if (!string.IsNullOrEmpty(found[7].Value))
            {
                firstTrend.WeatherCodes = found[7].Value + " ";
            }

            if (!string.IsNullOrEmpty(found[8].Value))
            {
                firstTrend.Clouds = found[8].Value + " ";
            }

            result.Add("TrendForecast", firstTrend);

            // Optional second forecat
            // if (!string.IsNullOrEmpty(found[6].Value))
            // {
            //     var additionalTrend = new TrendForecast
            //     {
            //         ChangeIndicator = found[7].Value switch
            //         {
            //             "NOSIG" => TrendForecastType.NoSignificantChanges,
            //             "BECMG" => TrendForecastType.Becoming,
            //             "TEMPO" => TrendForecastType.Temporary,
            //             _ => throw new ArgumentException("Invalid ChangeIndicator"),
            //         }
            //     };
            //
            //     if (!string.IsNullOrEmpty(found[8].Value))
            //     {
            //         additionalTrend.AtTime = found[8].Value;
            //     }
            //
            //     if (!string.IsNullOrEmpty(found[9].Value))
            //     {
            //         additionalTrend.FromTime = found[9].Value;
            //     }
            //
            //     if (!string.IsNullOrEmpty(found[10].Value))
            //     {
            //         additionalTrend.UntilTime = found[10].Value;
            //     }
            //
            //     if (!string.IsNullOrEmpty(found[11].Value))
            //     {
            //         // Prefix the forecasts with a fake airport ID for later parsing with the METAR decoder.
            //         additionalTrend.Forecast = $"ZZZZ {found[11].Value.Trim()}";
            //     }
            //
            //     result.Add("TrendForecastAdditional", additionalTrend);
            // }
        }

        return GetResults(newRemainingMetar, result);
    }
}
