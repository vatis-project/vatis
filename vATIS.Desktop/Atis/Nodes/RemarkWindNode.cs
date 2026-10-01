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
        var format = Station?.AtisFormat.RemarkWind;
        var wind = metar.RemarkWind;
        if (format == null || wind == null)
        {
            return;
        }

        var direction = wind.Direction.HasValue ? $"{wind.Direction.Value:000}" : "VRB";
        var gust = wind.Gust.HasValue ? $"G{wind.Gust.Value:00}" : string.Empty;
        TextAtis = Apply(format.TextTemplate, wind.HeightFeet.ToString(), direction, $"{wind.Speed:00}{gust}");

        var voiceDirection = wind.Direction.HasValue ? wind.Direction.Value.ToSerialFormat(leadingZero: true) : "variable";
        var voiceSpeed = wind.Speed.ToSerialFormat();
        if (wind.Gust.HasValue)
        {
            voiceSpeed += $" gusting {wind.Gust.Value.ToSerialFormat()}";
        }

        VoiceAtis = Apply(format.VoiceTemplate, wind.HeightFeet.ToSerialFormat(), voiceDirection, voiceSpeed);
    }

    private static string Apply(string? template, string height, string direction, string speed)
    {
        return (template ?? string.Empty)
            .Replace("{height}", height, StringComparison.OrdinalIgnoreCase)
            .Replace("{dir}", direction, StringComparison.OrdinalIgnoreCase)
            .Replace("{speed}", speed, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override string ParseVoiceVariables(RemarkWind node, string? format) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override string ParseTextVariables(RemarkWind node, string? format) =>
        throw new NotImplementedException();
}
