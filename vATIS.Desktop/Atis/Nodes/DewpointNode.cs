// <copyright file="DewpointNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;
public class DewpointNode : BaseNode<Value>
{
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.DewPointTemperature);
    }

    private void Parse(Value? node)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (node == null)
        {
            VoiceAtis = "Dewpoint missing";
            return;
        }

        VoiceAtis = ParseVoiceVariables(node, Station.AtisFormat.Dewpoint.Template.Voice);
        TextAtis = ParseTextVariables(node, Station.AtisFormat.Dewpoint.Template.Text);
    }

    public override string ParseTextVariables(Value value, string? format)
    {
        if (format == null)
            return "";

        return Regex.Replace(format, "{dewpoint}",
            string.Concat(value.ActualValue < 0 ? "M" : "", Math.Abs((int)value.ActualValue).ToString("00")),
            RegexOptions.IgnoreCase);
    }

    public override string ParseVoiceVariables(Value node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        return node.ActualValue < 0
            ? Regex.Replace(format, "{dewpoint}",
                "minus " + Math.Abs((int)node.ActualValue)
                    .ToString(Station.AtisFormat.Dewpoint.SpeakLeadingZero ? "00" : "").ToSerialFormat(),
                RegexOptions.IgnoreCase)
            : Regex.Replace(format, "{dewpoint}",
                (Station.AtisFormat.Dewpoint.UsePlusPrefix ? "plus " : "") + Math.Abs((int)node.ActualValue)
                    .ToString(Station.AtisFormat.Dewpoint.SpeakLeadingZero ? "00" : "").ToSerialFormat(),
                RegexOptions.IgnoreCase);
    }
}