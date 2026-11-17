// <copyright file="DensityAltitudeNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
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

        var magnitude = Math.Abs(altitude.Value);
        var digits = magnitude.ToString(CultureInfo.InvariantCulture);
        var isNegative = altitude.Value < 0;

        var textStyle = format.Format.Text.Style;
        var textPrefix = isNegative ? (textStyle is NumberStyle.Serial or NumberStyle.Group ? "MINUS " : "-") : string.Empty;
        var text = textPrefix + FormatNumber(digits, digits, format.Format.Text, true);

        var voicePrefix = isNegative ? "minus " : string.Empty;
        var voice = voicePrefix + FormatNumber(magnitude.ToSerialFormat(), digits, format.Format.Voice, false);

        TextAtis = Apply(format.TextTemplate, text);
        VoiceAtis = Apply(format.VoiceTemplate, voice);
    }

    /// <inheritdoc />
    public override string ParseVoiceVariables(int node, string? format) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public override string ParseTextVariables(int node, string? format) =>
        throw new NotImplementedException();

    // Group form leaves a trailing space, so trim the number before adding the suffix as typed.
    private static string FormatNumber(string rendered, string digits, OutputFormat output, bool uppercase)
    {
        var number = VariableFormatter.Apply(
            rendered, digits, new OutputFormat { Style = output.Style }, uppercase).Trim();
        return number + output.Suffix;
    }

    private static string Apply(string? template, string altitude)
    {
        return (template ?? string.Empty).Replace("{altitude}", altitude, StringComparison.OrdinalIgnoreCase);
    }
}
