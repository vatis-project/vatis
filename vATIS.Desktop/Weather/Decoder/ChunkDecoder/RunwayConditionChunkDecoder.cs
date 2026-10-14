// <copyright file="RunwayConditionChunkDecoder.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Decodes runway state groups (e.g. <c>R25/290150</c>) found in the body of a METAR.
/// Groups that carry no condition (<c>CLRD//</c>, <c>CLRD60</c>, <c>///////</c>) are consumed but ignored.
/// </summary>
public sealed class RunwayConditionChunkDecoder : MetarChunkDecoder
{
    private const string RunwayConditionsParameterName = "RunwayConditions";
    private const string GroupPattern = "R[0-9]{2}[LCR]?/(?:[0-9/]{6}|CLRD(?:[0-9]{2}|//))";

    private static readonly Regex s_groupRegex = new(
        "R(?<rwy>[0-9]{2}[LCR]?)/(?<a>[0-9/])(?<b>[0-9/])(?<cc>[0-9/]{2})(?<dd>[0-9/]{2})",
        RegexOptions.None,
        TimeSpan.FromMilliseconds(500));

    /// <inheritdoc/>
    public override string GetRegex()
    {
        return $"^(({GroupPattern}) )+";
    }

    /// <inheritdoc/>
    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var result = new Dictionary<string, object?>();

        if (found.Count > 1 && found[0].Success)
        {
            var conditions = new List<RunwayCondition>();

            foreach (Match match in s_groupRegex.Matches(found[0].Value))
            {
                var runwayNumber = int.Parse(match.Groups["rwy"].Value[..2], CultureInfo.InvariantCulture);
                if (runwayNumber is < 1 or > 36)
                {
                    continue;
                }

                var condition = Decode(match);
                if (condition != null)
                {
                    conditions.Add(condition);
                }
            }

            result.Add(RunwayConditionsParameterName, conditions);
        }

        return GetResults(consumed.Key, result);
    }

    private static RunwayCondition? Decode(Match match)
    {
        var a = match.Groups["a"].Value;
        if (a == "/")
        {
            return null;
        }

        var condition = new RunwayCondition
        {
            Runway = match.Groups["rwy"].Value,
            RawValue = match.Value,
            Deposit = (RunwayCondition.DepositType)int.Parse(a, CultureInfo.InvariantCulture),
        };

        var cc = match.Groups["cc"].Value;
        if (int.TryParse(cc, NumberStyles.None, CultureInfo.InvariantCulture, out var depth))
        {
            switch (depth)
            {
                case 99:
                    condition.NotOperational = true;
                    break;
                case >= 1 and <= 90:
                    condition.DepthMillimeters = depth;
                    break;
                case >= 92 and <= 98:
                    condition.DepthMillimeters = 100 + ((depth - 92) * 50);
                    break;
            }
        }

        var dd = match.Groups["dd"].Value;
        if (int.TryParse(dd, NumberStyles.None, CultureInfo.InvariantCulture, out var friction))
        {
            switch (friction)
            {
                case <= 90:
                    var coefficient = friction / 100.0;
                    condition.FrictionCoefficient = coefficient;
                    condition.Friction = coefficient switch
                    {
                        >= 0.40 => RunwayCondition.FrictionType.Good,
                        >= 0.36 => RunwayCondition.FrictionType.MediumToGood,
                        >= 0.30 => RunwayCondition.FrictionType.Medium,
                        >= 0.26 => RunwayCondition.FrictionType.MediumToPoor,
                        >= 0.17 => RunwayCondition.FrictionType.Poor,
                        _ => RunwayCondition.FrictionType.Unreliable,
                    };
                    break;
                case 91:
                    condition.Friction = RunwayCondition.FrictionType.Poor;
                    break;
                case 92:
                    condition.Friction = RunwayCondition.FrictionType.MediumToPoor;
                    break;
                case 93:
                    condition.Friction = RunwayCondition.FrictionType.Medium;
                    break;
                case 94:
                    condition.Friction = RunwayCondition.FrictionType.MediumToGood;
                    break;
                case 95:
                    condition.Friction = RunwayCondition.FrictionType.Good;
                    break;
                case 99:
                    condition.Friction = RunwayCondition.FrictionType.Unreliable;
                    break;
            }
        }

        return condition;
    }
}
