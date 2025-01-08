using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;
public class TemperatureNode : BaseNode<Value>
{
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.AirTemperature);
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

    public override string ParseTextVariables(Value value, string? format)
    {
        if (format == null)
            return "";
    
        // Parse the input format.
        // Matches patterns like {temp}, {temp:00}, {temp:00P}, or {temp:P}.
        // - "P" (optional) suppresses the "M" prefix for negative values.
        // - Numeric digits (optional) specify the minimum number of digits to display.
        var match = Regex.Match(format, @"\{temp(?::(?<format>[0]*P?))?\}");
        if (!match.Success)
        {
            throw new ArgumentException("Invalid temperature format string: " + format);
        }

        // Extract the format part (e.g., "00" or "00P")
        string formatting = match.Groups["format"].Value;
    
        // Check if the "P" parameter is present, which suppresses the "M" prefix
        bool suppressM = formatting.Contains('P');
    
        // Remove "P" from the format to get the numeric part (if any)
        string digitFormat = formatting.Replace("P", "").Trim();
    
        // Default to "00" if no digit format is provided
        if (string.IsNullOrEmpty(digitFormat)) digitFormat = "00";
    
        // Format the temperature
        if (value.ActualValue < 0 && !suppressM) // Only prefix "M" if negative and not suppressed
        {
            return $"M{Math.Abs(value.ActualValue).ToString(digitFormat)}";
        }
        return Math.Abs(value.ActualValue).ToString(digitFormat);
    }

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
}