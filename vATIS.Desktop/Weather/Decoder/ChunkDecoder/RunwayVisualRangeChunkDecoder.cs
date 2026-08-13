// <copyright file="RunwayVisualRangeChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using static Vatsim.Vatis.Weather.Decoder.Entity.RunwayVisualRange;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Provides functionality to decode METAR runway visual range (RVR) information
/// into structured data. This class is responsible for identifying and parsing
/// RVR-related chunks of a METAR report.
/// </summary>
/// <remarks>
/// This class extends the <see cref="MetarChunkDecoder"/> abstract class and
/// overrides relevant methods to implement decoding for RVR-specific METAR segments.
/// </remarks>
public sealed class RunwayVisualRangeChunkDecoder : MetarChunkDecoder
{
    private const string RunwaysVisualRangeParameterName = "RunwaysVisualRange";
    private const string RunwayRegexPattern = "R(\\d{2})([LRC]?)/(?:(M|P)?(\\d{4})|(////))(V)?(M|P)?(\\d{4})?([UDN])?";

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return $"^(({RunwayRegexPattern}) )+";
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
            var rvrMetarPart = found[0].Value;
            var chunkRegex = new Regex(RunwayRegexPattern, RegexOptions.None, TimeSpan.FromMilliseconds(500));
            var matches = chunkRegex.Matches(rvrMetarPart);

            var runways = new List<RunwayVisualRange>();

            foreach (Match match in matches)
            {
                var g = match.Groups;

                var rawValue = g[0].Value; // Full matched RVR string, e.g. "R19/0350VP1200"
                var runwayNumber = g[1].Value; // Runway number (2 digits), e.g. "19"
                var runwaySuffix = g[2].Value; // Runway letter suffix (optional), e.g. "L", "R", "C", or empty
                var prefix1 = g[3].Value; // Inequality prefix for first value (optional): "M" (less than), "P" (greater than)
                var value1 = g[4].Value; // First visual range value (4 digits), e.g. "0350"
                var isMissing = !string.IsNullOrEmpty(g[5].Value); // Missing value indicator: "////"
                var hasVariable = !string.IsNullOrEmpty(g[6].Value); // Variable indicator "V" present or not
                var prefix2 = g[7].Value; // Inequality prefix for second value (optional): "M" or "P"
                var value2 = g[8].Value; // Second visual range value (optional, 4 digits)
                var tendencyStr = g[9].Value; // Past tendency indicator (optional): "U", "D", or "N"

                var rvr = new RunwayVisualRange
                {
                    Runway = runwayNumber,
                    RunwaySuffix = runwaySuffix,
                    RawValue = rawValue,
                    IsMissing = isMissing,
                    Variable = hasVariable,
                    PastTendency = tendencyStr switch
                    {
                        "U" => Tendency.U,
                        "D" => Tendency.D,
                        "N" => Tendency.N,
                        _ => Tendency.None
                    }
                };

                if (!isMissing)
                {
                    var unit = Value.Unit.Meter;

                    if (hasVariable)
                    {
                        var min = string.IsNullOrEmpty(value1) ? null : new Value(Value.ToInt(value1)!.Value, unit);
                        var max = string.IsNullOrEmpty(value2) ? null : new Value(Value.ToInt(value2)!.Value, unit);
                        rvr.VisualRangeInterval = [min!, max!];
                        rvr.IsLessThan = prefix1 == "M";
                        rvr.IsGreaterThan = prefix2 == "P";
                    }
                    else
                    {
                        rvr.VisualRange = string.IsNullOrEmpty(value1)
                            ? null
                            : new Value(Value.ToInt(value1)!.Value, unit);
                        rvr.IsLessThan = prefix1 == "M";
                        rvr.IsGreaterThan = prefix1 == "P";
                    }
                }

                runways.Add(rvr);
            }

            result.Add(RunwaysVisualRangeParameterName, runways);
        }

        return GetResults(newRemainingMetar, result);
    }
}
