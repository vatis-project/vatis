// <copyright file="DewpointNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides the dewpoint temperature.
/// </summary>
public class DewpointNode : BaseNode<Value>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        this.Parse(metar.DewPointTemperature);
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
            "{dewpoint}",
            string.Concat(value.ActualValue < 0 ? "M" : string.Empty, Math.Abs((int)value.ActualValue).ToString("00")),
            RegexOptions.IgnoreCase);
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(Value node, string? format)
    {
        ArgumentNullException.ThrowIfNull(this.Station);
        ArgumentNullException.ThrowIfNull(this.Station.AtisFormat);
        ArgumentNullException.ThrowIfNull(node);

        if (format == null)
        {
            return string.Empty;
        }

        string FormatDewpoint(int value, bool isNegative)
        {
            var prefix = isNegative
                ? "minus "
                : (this.Station.AtisFormat.Dewpoint.UsePlusPrefix ? "plus " : string.Empty);
            var stringFormat = this.Station.AtisFormat.Dewpoint.SpeakLeadingZero ? "00" : string.Empty;
            return $"{prefix}{Math.Abs(value).ToString(stringFormat).ToSerialFormat()}";
        }

        return Regex.Replace(
            format,
            "{dewpoint}",
            FormatDewpoint((int)node.ActualValue, node.ActualValue < 0),
            RegexOptions.IgnoreCase);
    }

    private void Parse(Value? node)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        if (node == null)
        {
            this.VoiceAtis = "Dewpoint missing";
            return;
        }

        this.VoiceAtis = this.ParseVoiceVariables(node, this.Station.AtisFormat.Dewpoint.Template.Voice);
        this.TextAtis = this.ParseTextVariables(node, this.Station.AtisFormat.Dewpoint.Template.Text);
    }
}
