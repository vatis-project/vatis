// <copyright file="RunwayVisualRangeNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
            var result = new List<string>();

            if (rvr.RawValue == null)
            {
                continue;
            }

            var match = Regex.Match(rvr.RawValue,
                @"^R([0-3]{1}\d{1})(L|C|R)?\/(M|P)?(\d{4})(V|VP)?(\d{4})?(FT)?(?:\/?(U|D|N))?$");

            if (!match.Success)
            {
                continue;
            }

            acars.Add(rvr.RawValue);

            var rwyNumber = match.Groups[1].Value;

            var rwyDesignator = match.Groups[2].Value switch
            {
                "L" => "left",
                "R" => "right",
                "C" => "center",
                _ => ""
            };

            switch (match.Groups[5].Value)
            {
                case "V":
                {
                    var minVis = int.Parse(match.Groups[4].Value);
                    var maxVis = int.Parse(match.Groups[6].Value);

                    result.Add(match.Groups[3].Value == "M"
                        ? $"variable from less than {minVis.ToGroupForm()} to {maxVis.ToGroupForm()}"
                        : $"variable between {minVis.ToGroupForm()} and {maxVis.ToGroupForm()}");
                    break;
                }

                case "VP":
                {
                    var minVis = int.Parse(match.Groups[4].Value);
                    var maxVis = int.Parse(match.Groups[6].Value);

                    result.Add(match.Groups[3].Value == "M"
                        ? $"variable from less than {minVis.ToGroupForm()} to greater than{maxVis.ToGroupForm()}"
                        : $"{minVis.ToGroupForm()} variable to greater than {maxVis.ToGroupForm()}");
                    break;
                }

                default:
                {
                    var vis = int.Parse(match.Groups[4].Value);

                    switch (match.Groups[3].Value)
                    {
                        case "M":
                            result.Add($"less than {vis.ToGroupForm()}");
                            break;
                        case "P":
                            result.Add($"more than {vis.ToGroupForm()}");
                            break;
                        default:
                            result.Add(vis.ToGroupForm());
                            break;
                    }

                    break;
                }
            }

            var tendency = match.Groups[8].Value switch
            {
                "N" => Station?.AtisFormat.RunwayVisualRange.NeutralTendency,
                "U" => Station?.AtisFormat.RunwayVisualRange.GoingUpTendency,
                "D" => Station?.AtisFormat.RunwayVisualRange.GoingDownTendency,
                _ => ""
            };

            if (!string.IsNullOrEmpty(tendency))
            {
                result.Add(tendency);
            }

            tts.Add($"Runway {rwyNumber.ToSerialFormat()} {rwyDesignator} R-V-R {string.Join(" ", result)}.");
        }

        TextAtis = string.Join(" ", acars);
        VoiceAtis = string.Join(" ", tts).TrimEnd('.');
    }
}
