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
        this.Parse(metar.AirTemperature);
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(Value value, string? format)
    {
        if (format == null)
        {
            return string.Empty;
        }

        return Regex.Replace(
            format,
            "{temp}",
            string.Concat(value.ActualValue < 0 ? "M" : string.Empty, Math.Abs(value.ActualValue).ToString("00")),
            RegexOptions.IgnoreCase);
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(Value node, string? format)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        if (format == null)
        {
            return string.Empty;
        }

        return node.ActualValue < 0
            ? Regex.Replace(
                format,
                "{temp}",
                "minus " + Math.Abs(node.ActualValue).ToString(this.Station.AtisFormat.Temperature.SpeakLeadingZero ? "00" : string.Empty).ToSerialFormat(),
                RegexOptions.IgnoreCase)
            : Regex.Replace(
                format,
                "{temp}",
                (this.Station.AtisFormat.Temperature.UsePlusPrefix ? "plus " : string.Empty) + Math.Abs(node.ActualValue).ToString(this.Station.AtisFormat.Temperature.SpeakLeadingZero ? "00" : string.Empty).ToSerialFormat(),
                RegexOptions.IgnoreCase);
    }

    private void Parse(Value? temperature)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        if (temperature == null)
        {
            this.VoiceAtis = "Temperature missing";
            return;
        }

        this.VoiceAtis = this.ParseVoiceVariables(temperature, this.Station.AtisFormat.Temperature.Template.Voice);
        this.TextAtis = this.ParseTextVariables(temperature, this.Station.AtisFormat.Temperature.Template.Text);
    }
}
