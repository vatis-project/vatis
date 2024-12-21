using System.Collections.Generic;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;
using static Vatsim.Vatis.Weather.Decoder.Entity.RunwayVisualRange;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class RunwayVisualRangeChunkDecoder : MetarChunkDecoder
{
    private const string RunwaysVisualRangeParameterName = "RunwaysVisualRange";
    private const string RunwayRegexPattern = "R([0-9]{2}[LCR]?)/([PM]?([0-9]{4})V)?[PM]?([0-9]{4})(FT)?/?([UDN]?)";

    public override string GetRegex()
    {
        return $"^({RunwayRegexPattern})( {RunwayRegexPattern})?( {RunwayRegexPattern})?( {RunwayRegexPattern})?( )";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        if (found.Count > 1)
        {
            var runways = new List<RunwayVisualRange>();
            // iterate on the results to get all runways visual range found
            for (int i = 1; i <= 20; i += 7)
            {
                if (!string.IsNullOrEmpty(found[i].Value))
                {
                    // check runway qfu validity
                    var qfuAsInt = Value.ToInt(found[i + 1].Value);
                    if (qfuAsInt > 36 || qfuAsInt < 1)
                    {
                        throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                            MetarChunkDecoderException.Messages.INVALID_RUNWAY_QFU_RUNWAY_VISUAL_RANGE_INFORMATION);
                    }

                    // get distance unit
                    var rangeUnit = Value.Unit.Meter;
                    if (found[i + 5].Value == "FT")
                    {
                        rangeUnit = Value.Unit.Feet;
                    }
                    Tendency tendency = Tendency.None;
                    switch (found[i + 6].Value)
                    {
                        case "U":
                            tendency = Tendency.U;
                            break;

                        case "D":
                            tendency = Tendency.D;
                            break;

                        case "N":
                            tendency = Tendency.N;
                            break;
                    }
                    var observation = new RunwayVisualRange()
                    {
                        Runway = found[i + 1].Value,
                        PastTendency = tendency,
                        RawValue = found[i].Value
                    };

                    if (!string.IsNullOrEmpty(found[i + 3].Value))
                    {
                        observation.Variable = true;
                        var min = string.IsNullOrEmpty(found[i + 3].Value) ? null : new Value(Value.ToInt(found[i + 3].Value)!.Value, rangeUnit);
                        var max = string.IsNullOrEmpty(found[i + 4].Value) ? null : new Value(Value.ToInt(found[i + 4].Value)!.Value, rangeUnit);
                        if (max != null && min != null) 
                            observation.VisualRangeInterval = [min, max];
                    }
                    else
                    {
                        observation.Variable = false;
                        var v = string.IsNullOrEmpty(found[i + 4].Value) ? null : new Value(Value.ToInt(found[i + 4].Value)!.Value, rangeUnit);
                        if (v != null) observation.VisualRange = v;
                    }
                    runways.Add(observation);
                }
            }
            result.Add(RunwaysVisualRangeParameterName, runways);
        }

        return GetResults(newRemainingMetar, result);
    }
}