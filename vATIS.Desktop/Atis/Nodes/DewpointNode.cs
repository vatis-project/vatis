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

        // handle {prefix_symbol}
        format = Regex.Replace(format, @"\{prefix_symbol\}", value.ActualValue < 0 ? "-" : "+", RegexOptions.IgnoreCase);

        // Parse the input format using regex to match patterns like {temp}, {temp:##}, {temp:##M}, or {temp:M}
        var match = Regex.Match(format, @"\{dewpoint(?::(?<format>#*M?))?\}");
        if (!match.Success)
        {
            throw new ArgumentException("Invalid dewpoint format string: " + format);
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

        // Format the dewpoint value
        string formattedValue;
        string formatString = new string('0', digitCount); // Create a format string like "00", "000", etc.

        if (value.ActualValue < 0 && !suppressM) // Only prefix "M" if negative and not suppressed
        {
            formattedValue = $"M{Math.Abs(value.ActualValue).ToString(formatString)}";
        }
        else
        {
            formattedValue = Math.Abs(value.ActualValue).ToString(formatString);
        }

        // Replace the {dewpoint} variable in the format string with the formatted value
        format = format.Replace(match.Value, formattedValue);

        return format;
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