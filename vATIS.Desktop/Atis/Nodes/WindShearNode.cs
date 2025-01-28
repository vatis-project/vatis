// <copyright file="WindShearNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides wind shear information.
/// </summary>
public class WindShearNode : BaseNode<string>
{
    /// <inheritdoc />
    public override void Parse(DecodedMetar metar)
    {
        if (metar.WindshearAllRunways.HasValue && metar.WindshearAllRunways.Value)
        {
            TextAtis = "WS ALL RWY";
            VoiceAtis = "WIND SHEAR ALL RUNWAYS";
        }
        else if (metar.WindshearRunways != null)
        {
            var voiceResult = new List<string>();
            var textResult = new List<string>();

            foreach (var runway in metar.WindshearRunways)
            {
                textResult.Add($"WS R{runway}");

                var match = Regex.Match(runway, @"(\d{2})([LCR]?)");
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out var runwayNumber))
                    {
                        var runwaySide = match.Groups[2].Value switch
                        {
                            "L" => " LEFT",
                            "R" => " RIGHT",
                            "C" => " CENTER",
                            _ => ""
                        };

                        voiceResult.Add(
                            $"WIND SHEAR RUNWAY {runwayNumber.ToSerialFormat(leadingZero: true)}{runwaySide}");
                    }
                }
            }

            VoiceAtis = string.Join(", ", voiceResult);
            TextAtis = string.Join(" ", textResult);
        }
    }

    /// <inheritdoc />
    public override string ParseVoiceVariables(string node, string? format) =>
        throw new System.NotImplementedException();

    /// <inheritdoc />
    public override string ParseTextVariables(string node, string? format) =>
        throw new System.NotImplementedException();
}
