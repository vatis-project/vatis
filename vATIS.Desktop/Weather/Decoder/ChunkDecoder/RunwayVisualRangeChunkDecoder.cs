using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        return $"^(({RunwayRegexPattern}) )+";
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();

        if (found.Count > 1)
        {
            var rvrMetarPart = found[0].Value;
            var chunkRegex = new Regex($"^(({RunwayRegexPattern}) )", RegexOptions.None, TimeSpan.FromMilliseconds(500));
            var runways = new List<RunwayVisualRange>();
            // iterate on the results to get all runways visual range found
            List<Group> rvrRunwayGroups;
            while ((rvrRunwayGroups = chunkRegex.Match(rvrMetarPart).Groups.Cast<Group>().ToList()).Count > 1)
            {
                rvrMetarPart = chunkRegex.Replace(rvrMetarPart, string.Empty);
                var rvrRawValue = rvrRunwayGroups[2].Value;
                if (!string.IsNullOrEmpty(rvrRawValue))
                {
                    var rwyValue = rvrRunwayGroups[3].Value;
                    var minVisualRangeIntervalValue = rvrRunwayGroups[5].Value;
                    var maxVisualRangeIntervalValue = rvrRunwayGroups[6].Value;
                    var rangeUnitValue = rvrRunwayGroups[7].Value;
                    var tendencyValue = rvrRunwayGroups[8].Value;
                    
                    // check runway qfu validity
                    var qfuAsInt = Value.ToInt(rwyValue);
                    if (qfuAsInt > 36 || qfuAsInt < 1)
                    {
                        throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                            MetarChunkDecoderException.Messages.INVALID_RUNWAY_QFU_RUNWAY_VISUAL_RANGE_INFORMATION);
                    }

                    // get distance unit
                    var rangeUnit = Value.Unit.Meter;
                    if (rangeUnitValue == "FT")
                    {
                        rangeUnit = Value.Unit.Feet;
                    }
                    Tendency tendency = Tendency.None;
                    switch (tendencyValue)
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
                        Runway = rwyValue,
                        PastTendency = tendency,
                        RawValue = rvrRawValue
                    };

                    if (!string.IsNullOrEmpty(minVisualRangeIntervalValue))
                    {
                        observation.Variable = true;
                        var min = string.IsNullOrEmpty(minVisualRangeIntervalValue) ? null : new Value(Value.ToInt(minVisualRangeIntervalValue)!.Value, rangeUnit);
                        var max = string.IsNullOrEmpty(maxVisualRangeIntervalValue) ? null : new Value(Value.ToInt(maxVisualRangeIntervalValue)!.Value, rangeUnit);
                        if (max != null && min != null) 
                            observation.VisualRangeInterval = [min, max];
                    }
                    else
                    {
                        observation.Variable = false;
                        var v = string.IsNullOrEmpty(maxVisualRangeIntervalValue) ? null : new Value(Value.ToInt(maxVisualRangeIntervalValue)!.Value, rangeUnit);
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