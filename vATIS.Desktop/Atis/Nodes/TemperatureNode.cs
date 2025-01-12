﻿// <copyright file="TemperatureNode.cs" company="Justin Shannon">
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

        // handle {prefix_symbol}
        format = Regex.Replace(format, @"\{prefix_symbol\}", value.ActualValue < 0 ? "-" : "+", RegexOptions.IgnoreCase);

        // Parse the input format using regex to match patterns like {temp}, {temp:##}, {temp:##M}, or {temp:M}
        var match = Regex.Match(format, @"\{temp(?::(?<format>#*M?))?\}");
        if (!match.Success)
        {
            throw new ArgumentException("Invalid temperature format string: " + format);
        }

        // Extract the format part (e.g., "##" or "##M")
        string formatting = match.Groups["format"].Value;

        // Check if the "M" character is present, indicating whether to suppress the "M" prefix
        bool suppressM = formatting.Contains('M');

        // Remove "M" from the format (if present) to get the numeric part
        string digitFormat = formatting.Replace("M", "").Trim();

        // Default to "00" if no digit format is provided (i.e., no '#' symbols)
        if (string.IsNullOrEmpty(digitFormat)) digitFormat = "00";

        // Count the number of '#' symbols to determine the format (e.g., 2 '#' symbols -> "00")
        int digitCount = digitFormat.Length;

        // Format the temperature value
        string formattedValue;
        string formatString = new string('0', digitCount); // Create a format string like "00", "000", etc.

        // Only prefix "M" if negative and not suppressed
        if (value.ActualValue < 0 && !suppressM)
        {
            formattedValue = $"M{Math.Abs(value.ActualValue).ToString(formatString)}";
        }
        else
        {
            formattedValue = Math.Abs(value.ActualValue).ToString(formatString);
        }

        // Replace the temperature placeholder in the format string with the formatted value
        format = format.Replace(match.Value, formattedValue);

        return format;
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
