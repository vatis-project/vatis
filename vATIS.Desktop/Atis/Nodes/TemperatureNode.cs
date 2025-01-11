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

        if (Station == null)
            return "";

        format = Regex.Replace(format, "{temp}", string.Concat(value.ActualValue < 0 && Station.IsFaaAtis ? "M" : "",
            Math.Abs(value.ActualValue).ToString("00")), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{prefix_symbol}", value.ActualValue < 0 ? "-" : "+", RegexOptions.IgnoreCase);

        return format;
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