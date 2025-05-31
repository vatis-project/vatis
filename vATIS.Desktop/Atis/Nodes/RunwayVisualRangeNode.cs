// <copyright file="RunwayVisualRangeNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides runway visual range information.
/// </summary>
public class RunwayVisualRangeNode : BaseNode<RunwayVisualRange>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.RunwaysVisualRange);
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(RunwayVisualRange value, string? format)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(RunwayVisualRange node, string? format)
    {
        throw new NotImplementedException();
    }

    private void Parse(List<RunwayVisualRange> runwayVisualRanges)
    {
        var tts = new List<string>();
        var acars = new List<string>();

        foreach (var rvr in runwayVisualRanges)
        {
            if (rvr.RawValue == null)
                continue;

            acars.Add(rvr.RawValue);

            var result = new List<string>();

            // Runway
            var rwyDesignatorText = rvr.RunwaySuffix switch
            {
                "L" => "left",
                "R" => "right",
                "C" => "center",
                _ => ""
            };
            var runwayString = $"Runway {rvr.Runway.ToSerialFormat()}";
            if (!string.IsNullOrEmpty(rwyDesignatorText))
                runwayString += $" {rwyDesignatorText}";

            if (rvr.IsMissing)
            {
                result.Add("missing");
            }
            else
            {
                if (rvr is { Variable: true, VisualRangeInterval.Length: 2 })
                {
                    var minValue = rvr.VisualRangeInterval[0];
                    var maxValue = rvr.VisualRangeInterval[1];

                    var minText = minValue.ActualValue.ToGroupForm();
                    var maxText = maxValue.ActualValue.ToGroupForm();

                    if (rvr.IsGreaterThan)
                    {
                        result.Add($"{minValue.ActualValue.ToGroupForm()} variable to " +
                                   $"greater than {maxValue.ActualValue.ToGroupForm()}");
                    }
                    else
                    {
                        result.Add($"variable between {minText} and {maxText}");
                    }
                }
                else if (rvr.VisualRange != null)
                {
                    var visText = rvr.VisualRange.ActualValue.ToGroupForm();

                    if (rvr.IsLessThan)
                    {
                        result.Add($"less than {visText}");
                    }
                    else if (rvr.IsGreaterThan)
                    {
                        result.Add($"more than {visText}");
                    }
                    else
                    {
                        result.Add(visText);
                    }
                }

                // Handle past-tendency
                var tendencyText = rvr.PastTendency switch
                {
                    RunwayVisualRange.Tendency.N => Station?.AtisFormat.RunwayVisualRange.NeutralTendency,
                    RunwayVisualRange.Tendency.U => Station?.AtisFormat.RunwayVisualRange.GoingUpTendency,
                    RunwayVisualRange.Tendency.D => Station?.AtisFormat.RunwayVisualRange.GoingDownTendency,
                    _ => ""
                };

                if (!string.IsNullOrEmpty(tendencyText))
                {
                    result.Add(tendencyText);
                }
            }

            tts.Add($"{runwayString} R-V-R {string.Join(" ", result)}.");
        }

        TextAtis = string.Join(" ", acars);
        VoiceAtis = string.Join(" ", tts).TrimEnd('.');
    }
}
