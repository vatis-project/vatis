// <copyright file="RemarkWindNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides the wind reported in the METAR remarks.
/// </summary>
public class RemarkWindNode : BaseNode<RemarkWind>
{
    /// <inheritdoc />
    public override void Parse(DecodedMetar metar)
    {
        var wind = metar.RemarkWind;
        if (wind == null)
        {
            return;
        }

        var direction = wind.Direction.HasValue ? $"{wind.Direction.Value:000}" : "VRB";
        var gust = wind.Gust.HasValue ? $"G{wind.Gust.Value:00}" : string.Empty;
        TextAtis = $"WIND {wind.HeightFeet}FT {direction}/{wind.Speed:00}{gust}KT";

        var voiceDirection = wind.Direction.HasValue
            ? $"{wind.Direction.Value.ToSerialFormat(leadingZero: true)} DEGREES"
            : "VARIABLE";
        var voiceGust = wind.Gust.HasValue ? $" GUSTING {wind.Gust.Value.ToSerialFormat()}" : string.Empty;
        VoiceAtis =
            $"WIND {wind.HeightFeet.ToSerialFormat()} FEET {voiceDirection} {wind.Speed.ToSerialFormat()}{voiceGust} KNOTS";
    }

    /// <inheritdoc />
    public override string ParseVoiceVariables(RemarkWind node, string? format) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override string ParseTextVariables(RemarkWind node, string? format) =>
        throw new NotImplementedException();
}
