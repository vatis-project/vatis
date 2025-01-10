using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class WindShearChunkDecoder : MetarChunkDecoder
{
    private const string WindshearAllRunwaysParameterName = "WindshearAllRunways";
    private const string WindshearRunwaysParameterName = "WindshearRunways";

    private const string RunwayRegexPattern = "WS R(WY)?([0-9]{2}[LCR]?)";

    public override string GetRegex()
    {
        return $"^(WS ALL RWY|({RunwayRegexPattern})( {RunwayRegexPattern})?( {RunwayRegexPattern})?)( )";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        bool? all = null;
        var runways = new List<string>();

        if (found.Count > 1)
        {
            // detect if we have windshear on all runway or only one
            if (found[1].Value == "WS ALL RWY")
            {
                all = true;
                runways = null;
            }
            else
            {
                // one or more runways, build array
                all = false;

                for (var k = 2; k < 9; k += 3)
                {
                    if (!string.IsNullOrEmpty(found[k].Value))
                    {
                        var runway = found[k + 2].Value;
                        var qfuAsInt = Value.ToInt(runway);
                        // check runway qfu validity
                        if (qfuAsInt > 36 || qfuAsInt < 1)
                        {
                            throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                                MetarChunkDecoderException.Messages.InvalidRunwayQfuRunwaVisualRangeInformation);
                        }
                        runways.Add(runway);
                    }
                }
            }
        }

        result.Add(WindshearAllRunwaysParameterName, all);
        result.Add(WindshearRunwaysParameterName, runways);

        return GetResults(newRemainingMetar, result);
    }
}