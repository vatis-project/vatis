// <copyright file="PresentWeatherChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class PresentWeatherChunkDecoder : MetarChunkDecoder
{
    private const string PresentWeatherParameterName = "PresentWeather";
    public const string CaracRegexPattern = "TS|FZ|SH|BL|DR|MI|BC|PR";
    public const string TypeRegexPattern = "DZ|RA|SN|SG|PL|DS|GR|GS|UP|IC|FG|BR|SA|DU|HZ|FU|VA|PY|DU|PO|SQ|FC|DS|SS|//";

    public override string GetRegex()
    {
        const string presentWeatherRegexPattern = $"([-+]|VC)?({CaracRegexPattern})?({TypeRegexPattern})?({TypeRegexPattern})?({TypeRegexPattern})?";
        return $"^({presentWeatherRegexPattern} )?({presentWeatherRegexPattern} )?({presentWeatherRegexPattern} )?()?";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        var presentWeather = new List<WeatherPhenomenon>();

        for (var i = 1; i <= 13; i += 6)
        {
            if (!string.IsNullOrEmpty(found[i].Value) && found[i + 3].Value != "//")
            {
                var weather = new WeatherPhenomenon()
                {
                    IntensityProximity = found[i + 1].Value,
                    Characteristics = found[i + 2].Value,
                    RawValue = found[i].Value
                };
                for (var k = 3; k <= 5; ++k)
                {
                    if (!string.IsNullOrEmpty(found[i + k].Value))
                    {
                        weather.AddType(found[i + k].Value);
                    }
                }
                presentWeather.Add(weather);
            }
        }

        result.Add(PresentWeatherParameterName, presentWeather);

        return GetResults(newRemainingMetar, result);
    }
}
