// <copyright file="DensityAltitudeNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides the density altitude reported in the METAR remarks.
/// </summary>
public class DensityAltitudeNode : BaseNode<int>
{
    /// <inheritdoc />
    public override void Parse(DecodedMetar metar)
    {
        var format = Station?.AtisFormat.DensityAltitude;
        var altitude = metar.DensityAltitudeFeet;
        if (format == null || altitude == null)
        {
            return;
        }

        var text = altitude.Value.ToString();
        var voice = altitude.Value.ToSerialFormat();
        if (altitude.Value < 0)
        {
            voice = "minus " + Math.Abs(altitude.Value).ToSerialFormat();
        }

        TextAtis = Apply(format.TextTemplate, text);
        VoiceAtis = Apply(format.VoiceTemplate, voice);
    }

    /// <inheritdoc />
    public override string ParseVoiceVariables(int node, string? format) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override string ParseTextVariables(int node, string? format) =>
        throw new NotImplementedException();

    private static string Apply(string? template, string altitude)
    {
        return (template ?? string.Empty).Replace("{altitude}", altitude, StringComparison.OrdinalIgnoreCase);
    }
}
