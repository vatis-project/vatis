﻿using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;
public class ObservationTimeNode : BaseNode<string>
{
    private const string SpecialText = "SPECIAL";
    private bool _isSpecialAtis;

    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.Hour, metar.Minute);
    }

    private void Parse(int metarHour, int metarMinute)
    {
        ArgumentNullException.ThrowIfNull(Station);

        _isSpecialAtis = Station.AtisFormat.ObservationTime.StandardUpdateTime != null
                         && !Station.AtisFormat.ObservationTime.StandardUpdateTime.Contains(metarMinute);

        VoiceAtis = ParseVoiceVariables(metarHour, metarMinute, Station.AtisFormat.ObservationTime.Template.Voice);
        TextAtis = ParseTextVariables(metarHour, metarMinute, Station.AtisFormat.ObservationTime.Template.Text);
    }

    private string ParseTextVariables(int metarHour, int metarMinute, string? format)
    {
        if (format == null)
            return "";

        format = Regex.Replace(format, "{time}", $"{metarHour:00}{metarMinute:00}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{hour}", $"{metarHour:00}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{minute}", $"{metarMinute:00}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{special}", _isSpecialAtis ? SpecialText : "", RegexOptions.IgnoreCase);

        return format;
    }

    private string ParseVoiceVariables(int metarHour, int metarMinute, string? format)
    {
        if (format == null)
            return "";

        format = Regex.Replace(format, "{time}", $"{metarHour.ToString("00").ToSerialFormat()} {metarMinute.ToString("00").ToSerialFormat()}", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{hour}", metarHour.ToString("00").ToSerialFormat() ?? string.Empty, RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{minute}", metarMinute.ToString("00").ToSerialFormat() ?? string.Empty, RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{special}", _isSpecialAtis ? SpecialText : "", RegexOptions.IgnoreCase);

        return format;
    }

    public override string ParseVoiceVariables(string node, string? format)
    {
        throw new NotImplementedException();
    }

    public override string ParseTextVariables(string value, string? format)
    {
        throw new NotImplementedException();
    }
}
