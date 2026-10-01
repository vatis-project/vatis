// <copyright file="TemperatureNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides the temperature.
/// </summary>
public class TemperatureNode : BaseNode<Value>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.AirTemperature);
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(Value value, string? format)
    {
        if (format == null)
            return "";

        return SignedValueFormatter.Format(format, "temp", value.ActualValue);
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(Value node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        return node.ActualValue < 0
            ? Regex.Replace(format, "{temp}",
                "minus " + Math.Abs(node.ActualValue)
                    .ToString(Station.AtisFormat.Temperature.SpeakLeadingZero ? "00" : "").ToSerialFormat(),
                RegexOptions.IgnoreCase)
            : Regex.Replace(format, "{temp}",
                (Station.AtisFormat.Temperature.UsePlusPrefix ? "plus " : "") + Math.Abs(node.ActualValue)
                    .ToString(Station.AtisFormat.Temperature.SpeakLeadingZero ? "00" : "").ToSerialFormat(),
                RegexOptions.IgnoreCase);
    }

    private void Parse(Value? temperature)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (temperature == null)
        {
            VoiceAtis = "Temperature missing";
            return;
        }

        VoiceAtis = ParseVoiceVariables(temperature, Station.AtisFormat.Temperature.Template.Voice);
        TextAtis = ParseTextVariables(temperature, Station.AtisFormat.Temperature.Template.Text);
    }
}
