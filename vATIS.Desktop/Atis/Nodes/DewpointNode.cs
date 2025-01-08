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
    
        // Parse the input format.
        // Matches patterns like {dewpoint}, {dewpoint:00}, {dewpoint:00P}, or {dewpoint:P}.
        // - "P" (optional) suppresses the "M" prefix for negative values.
        // - Numeric digits (optional) specify the minimum number of digits to display.
        var match = Regex.Match(format, @"\{dewpoint(?::(?<format>[0-9]*P?))?\}");
        if (!match.Success)
        {
            throw new ArgumentException("Invalid dewpoint format string: " + format);
        }

        // Extract the format part (e.g., "00" or "00P")
        string formatting = match.Groups["format"].Value;
    
        // Check if the "P" parameter is present, which suppresses the "M" prefix
        bool suppressM = formatting.Contains('P');
    
        // Remove "P" from the format to get the numeric part (if any)
        string digitFormat = formatting.Replace("P", "").Trim();
    
        // Default to "00" if no digit format is provided
        if (string.IsNullOrEmpty(digitFormat)) digitFormat = "00";
    
        // Format the dewpoint
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