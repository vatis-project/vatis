// <copyright file="TemperatureChunkDecoder.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Responsible for decoding the temperature-related information from a METAR string.
/// </summary>
/// <remarks>
/// This class parses the air temperature and dew point temperature reported in METAR data
/// by matching specific temperature-related patterns. It extends the functionality of the
/// <see cref="MetarChunkDecoder"/> class.
/// </remarks>
public sealed class TemperatureChunkDecoder : MetarChunkDecoder
{
    private const string AirTemperatureParameterName = "AirTemperature";
    private const string DewPointTemperatureParameterName = "DewPointTemperature";
    private const string TempRegexPattern = "(M?[0-9]{2})";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return $"^{TempRegexPattern}?/{TempRegexPattern}?( )";
    }

    /// <inheritdoc/>
    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = this.Consume(remainingMetar);
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

        return this.GetResults(newRemainingMetar, result);
    }
}
