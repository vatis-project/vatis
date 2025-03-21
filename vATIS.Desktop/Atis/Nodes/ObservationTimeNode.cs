// <copyright file="ObservationTimeNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides the observation time.
/// </summary>
public class ObservationTimeNode : BaseNode<string>
{
    private const string SpecialText = "SPECIAL";
    private bool _isSpecialAtis;

    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.Day, metar.Hour, metar.Minute);
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(string node, string? format)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(string value, string? format)
    {
        throw new NotImplementedException();
    }

    private void Parse(int metarDay, int metarHour, int metarMinute)
    {
        ArgumentNullException.ThrowIfNull(Station);

        _isSpecialAtis = Station.AtisFormat.ObservationTime.StandardUpdateTime != null
                         && !Station.AtisFormat.ObservationTime.StandardUpdateTime.Contains(metarMinute);

        VoiceAtis = ParseVoiceVariables(metarDay, metarHour, metarMinute, Station.AtisFormat.ObservationTime.Template.Voice);
        TextAtis = ParseTextVariables(metarDay, metarHour, metarMinute, Station.AtisFormat.ObservationTime.Template.Text);
    }

    private string ParseTextVariables(int metarDay, int metarHour, int metarMinute, string? format)
    {
        if (format == null)
            return "";

        format = Regex.Replace(format, "{day}", $"{metarDay:00}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{time}", $"{metarHour:00}{metarMinute:00}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{hour}", $"{metarHour:00}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{minute}", $"{metarMinute:00}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{special}", _isSpecialAtis ? SpecialText : "", RegexOptions.IgnoreCase);

        return format;
    }

    private string ParseVoiceVariables(int metarDay, int metarHour, int metarMinute, string? format)
    {
        if (format == null)
            return "";

        format = Regex.Replace(format, "{day}", metarDay.ToString("00").ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{time}", $"{metarHour.ToString("00").ToSerialFormat()} {metarMinute.ToString("00").ToSerialFormat()}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{hour}", metarHour.ToString("00").ToSerialFormat() ?? string.Empty, RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{minute}", metarMinute.ToString("00").ToSerialFormat() ?? string.Empty, RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{special}", _isSpecialAtis ? SpecialText : "", RegexOptions.IgnoreCase);

        return format;
    }
}
