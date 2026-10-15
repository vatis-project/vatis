// <copyright file="TemperatureNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
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

        return SignedValueFormatter.Format(
            format, "temp", value.ActualValue, Station?.AtisFormat.Temperature.Format.Text.Suffix ?? string.Empty);
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(Value node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        var settings = Station.AtisFormat.Temperature;
        var digits = Math.Abs(node.ActualValue).ToString(settings.SpeakLeadingZero ? "00" : "");
        var spokenNumber = settings.Format.Voice.Style switch
        {
            NumberStyle.Digits => digits,
            NumberStyle.Group => ((int)Math.Abs(node.ActualValue)).ToGroupForm(),
            _ => digits.ToSerialFormat(),
        };
        var spoken = spokenNumber + settings.Format.Voice.Suffix;

        var prefix = node.ActualValue < 0 ? "minus " : settings.UsePlusPrefix ? "plus " : string.Empty;
        return Regex.Replace(format, "{temp}", _ => prefix + spoken, RegexOptions.IgnoreCase);
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
