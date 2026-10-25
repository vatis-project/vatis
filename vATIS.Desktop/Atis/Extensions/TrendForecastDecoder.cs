// <copyright file="TrendForecastDecoder.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Decodes the raw groups of a <see cref="TrendForecast"/> into the same entities as a METAR.
/// </summary>
public static class TrendForecastDecoder
{
    /// <summary>
    /// Decodes the wind, visibility, weather and clouds of a trend.
    /// </summary>
    /// <param name="forecast">The trend forecast.</param>
    /// <param name="decoded">The METAR to fill in, or null to create one.</param>
    /// <returns>The decoded trend.</returns>
    public static DecodedMetar Decode(TrendForecast forecast, DecodedMetar? decoded = null)
    {
        decoded ??= new DecodedMetar();

        if (forecast.SurfaceWind != null)
        {
            Apply(new SurfaceWindChunkDecoder().Parse(forecast.SurfaceWind), decoded);
        }

        if (forecast.PrevailingVisibility != null)
        {
            if (forecast.PrevailingVisibility.Trim() == "CAVOK")
            {
                decoded.Cavok = true;
            }
            else
            {
                Apply(new VisibilityChunkDecoder().Parse(forecast.PrevailingVisibility), decoded);
            }
        }

        if (forecast.WeatherCodes != null)
        {
            Apply(new PresentWeatherChunkDecoder().Parse(forecast.WeatherCodes), decoded);
        }

        if (forecast.Clouds?.Length > 0)
        {
            Apply(new CloudChunkDecoder().Parse(forecast.Clouds), decoded);
        }

        return decoded;
    }

    private static void Apply(Dictionary<string, object> chunkResult, DecodedMetar decoded)
    {
        if (chunkResult.TryGetValue("Result", out var value) && value is Dictionary<string, object> result)
        {
            foreach (var obj in result)
            {
                typeof(DecodedMetar).GetProperty(obj.Key)?.SetValue(decoded, obj.Value, null);
            }
        }
    }
}
