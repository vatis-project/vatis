using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

public class TemperatureNode : BaseNode<Value>
{
    public override void Parse(DecodedMetar metar)
    {
        this.Parse(metar.AirTemperature);
    }

    private void Parse(Value? temperature)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        if (temperature == null)
        {
            this.VoiceAtis = "Temperature missing";
            return;
        }

        this.VoiceAtis = this.ParseVoiceVariables(temperature, this.Station.AtisFormat.Temperature.Template.Voice);
        this.TextAtis = this.ParseTextVariables(temperature, this.Station.AtisFormat.Temperature.Template.Text);
    }

    public override string ParseTextVariables(Value value, string? format)
    {
        if (format == null)
        {
            return "";
        }

        return Regex.Replace(
            format,
            "{temp}",
            string.Concat(value.ActualValue < 0 ? "M" : "", Math.Abs(value.ActualValue).ToString("00")),
            RegexOptions.IgnoreCase);
    }

    public override string ParseVoiceVariables(Value node, string? format)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        if (format == null)
        {
            return "";
        }

        return node.ActualValue < 0
            ? Regex.Replace(
                format,
                "{temp}",
                "minus " + Math.Abs(node.ActualValue)
                    .ToString(this.Station.AtisFormat.Temperature.SpeakLeadingZero ? "00" : "").ToSerialFormat(),
                RegexOptions.IgnoreCase)
            : Regex.Replace(
                format,
                "{temp}",
                (this.Station.AtisFormat.Temperature.UsePlusPrefix ? "plus " : "") + Math.Abs(node.ActualValue)
                    .ToString(this.Station.AtisFormat.Temperature.SpeakLeadingZero ? "00" : "").ToSerialFormat(),
                RegexOptions.IgnoreCase);
    }
}