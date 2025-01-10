using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Represents a decoder for recent weather information in a METAR report.
/// This class is responsible for parsing and interpreting recent weather-related
/// information based on specified decoding logic.
/// </summary>
public sealed class RecentWeatherChunkDecoder : MetarChunkDecoder
{
    private const string RecentWeatherParameterName = "RecentWeather";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return
            $"^RE({PresentWeatherChunkDecoder.CaracRegexPattern})?({PresentWeatherChunkDecoder.TypeRegexPattern})?({PresentWeatherChunkDecoder.TypeRegexPattern})?({PresentWeatherChunkDecoder.TypeRegexPattern})?()? ";
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
            // retrieve found params
            var weather = new WeatherPhenomenon
            {
                Characteristics = found[1].Value,
            };
            for (var k = 2; k <= 4; ++k)
            {
                if (!string.IsNullOrEmpty(found[k].Value))
                {
                    weather.AddType(found[k].Value);
                }
            }

            result.Add(RecentWeatherParameterName, weather);
        }

        return this.GetResults(newRemainingMetar, result);
    }
}
