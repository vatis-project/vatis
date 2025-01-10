using System;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

public class DewpointNode : BaseNode<Value>
{
    public override void Parse(DecodedMetar metar)
    {
        this.Parse(metar.DewPointTemperature);
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

    public override string ParseTextVariables(Value value, string? format)
    {
        if (format == null)
        {
            return "";
        }

        return Regex.Replace(
            format,
            "{dewpoint}",
            string.Concat(value.ActualValue < 0 ? "M" : "", Math.Abs((int)value.ActualValue).ToString("00")),
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
                "{dewpoint}",
                "minus " + Math.Abs((int)node.ActualValue)
                    .ToString(this.Station.AtisFormat.Dewpoint.SpeakLeadingZero ? "00" : "").ToSerialFormat(),
                RegexOptions.IgnoreCase)
            : Regex.Replace(
                format,
                "{dewpoint}",
                (this.Station.AtisFormat.Dewpoint.UsePlusPrefix ? "plus " : "") + Math.Abs((int)node.ActualValue)
                    .ToString(this.Station.AtisFormat.Dewpoint.SpeakLeadingZero ? "00" : "").ToSerialFormat(),
                RegexOptions.IgnoreCase);
    }
}