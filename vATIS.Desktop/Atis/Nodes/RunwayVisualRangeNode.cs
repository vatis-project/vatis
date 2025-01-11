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
        this.Parse(metar.RunwaysVisualRange);
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

            if (rvr.RawValue != null)
            {
                var match = Regex.Match(
                    rvr.RawValue,
                    @"^R([0-3]{1}\d{1})(L|C|R)?\/(M|P)?(\d{4})(V|VP)?(\d{4})?(FT)?(?:\/?(U|D|N))?$");

                if (match.Success)
                {
                    acars.Add(rvr.RawValue);

                    var rwyNumber = match.Groups[1].Value;
                    var rwyDesignator = string.Empty;

                    switch (match.Groups[2].Value)
                    {
                        case "L":
                            rwyDesignator = "left";
                            break;
                        case "R":
                            rwyDesignator = "right";
                            break;
                        case "C":
                            rwyDesignator = "center";
                            break;
                    }

                    if (match.Groups[5].Value == "V")
                    {
                        var minVis = int.Parse(match.Groups[4].Value);
                        var maxVis = int.Parse(match.Groups[6].Value);

                        result.Add(
                            match.Groups[3].Value == "M"
                                ? $"variable from less than {minVis.ToGroupForm()} to {maxVis.ToGroupForm()}"
                                : $"variable between {minVis.ToGroupForm()} and {maxVis.ToGroupForm()}");
                    }
                    else if (match.Groups[5].Value == "VP")
                    {
                        var minVis = int.Parse(match.Groups[4].Value);
                        var maxVis = int.Parse(match.Groups[6].Value);

                        result.Add(
                            match.Groups[3].Value == "M"
                                ? $"variable from less than {minVis.ToGroupForm()} to greater than{maxVis.ToGroupForm()}"
                                : $"{minVis.ToGroupForm()} variable to greater than {maxVis.ToGroupForm()}");
                    }
                    else
                    {
                        var vis = int.Parse(match.Groups[4].Value);

                        if (match.Groups[3].Value == "M")
                        {
                            result.Add($"less than {vis.ToGroupForm()}");
                        }
                        else if (match.Groups[3].Value == "P")
                        {
                            result.Add($"more than {vis.ToGroupForm()}");
                        }
                        else
                        {
                            result.Add(vis.ToGroupForm());
                        }
                    }

                    var tendency = string.Empty;
                    switch (match.Groups[8].Value)
                    {
                        case "N":
                            tendency = "neutral";
                            break;
                        case "U":
                            tendency = "going up";
                            break;
                        case "D":
                            tendency = "going down";
                            break;
                    }

                    result.Add(tendency);

                    tts.Add($"Runway {rwyNumber.ToSerialFormat()} {rwyDesignator} R-V-R {string.Join(" ", result)}.");
                }
            }
        }

        this.TextAtis = string.Join(" ", acars);
        this.VoiceAtis = string.Join(" ", tts).TrimEnd('.');
    }
}
