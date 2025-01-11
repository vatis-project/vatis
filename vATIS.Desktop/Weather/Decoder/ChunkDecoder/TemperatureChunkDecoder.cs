// <copyright file="TemperatureChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class TemperatureChunkDecoder : MetarChunkDecoder
{
    private const string AirTemperatureParameterName = "AirTemperature";
    private const string DewPointTemperatureParameterName = "DewPointTemperature";
    private const string TempRegexPattern = "(M?[0-9]{2})";

    public override string GetRegex()
    {
        return $"^{TempRegexPattern}?/{TempRegexPattern}?( )";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        if (found.Count > 1)
        {
            if (!string.IsNullOrEmpty(found[1].Value))
            {
                var multiplier = found[1].Value.StartsWith('M') ? -1 : 1;
                var value = int.Parse(found[1].Value.TrimStart('M')) * multiplier;
                result.Add(AirTemperatureParameterName, new Value(value, Value.Unit.DegreeCelsius));
            }

            if (!string.IsNullOrEmpty(found[2].Value))
            {
                var multiplier = found[2].Value.StartsWith('M') ? -1 : 1;
                var value = int.Parse(found[2].Value.TrimStart('M')) * multiplier;
                result.Add(DewPointTemperatureParameterName, new Value(value, Value.Unit.DegreeCelsius));
            }
        }

        return GetResults(newRemainingMetar, result);
    }
}