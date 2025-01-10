using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

public class CloudNode : BaseNode<CloudLayer>
{
    private CloudLayer? _ceilingLayer;

    public override void Parse(DecodedMetar metar)
    {
        this.Parse(metar.Clouds);
    }

    private void Parse(List<CloudLayer> cloudLayers)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        var voiceAtis = new List<string>();
        var textAtis = new List<string>();

        this._ceilingLayer = cloudLayers
            .Where(
                n => n.BaseHeight?.ActualValue > 0 &&
                     n.Amount is CloudLayer.CloudAmount.Overcast or CloudLayer.CloudAmount.Broken)
            .Select(n => n)
            .OrderBy(n => n.BaseHeight?.ActualValue)
            .FirstOrDefault();

        foreach (var layer in cloudLayers)
        {
            voiceAtis.Add(this.FormatCloudsVoice(layer));
            textAtis.Add(this.FormatCloudsText(layer));
        }

        var voiceTemplate = this.Station.AtisFormat.Clouds.Template.Voice;
        var textTemplate = this.Station.AtisFormat.Clouds.Template.Text;

        if (voiceTemplate != null)
        {
            this.VoiceAtis = voiceAtis.Count > 0
                ? Regex.Replace(
                    voiceTemplate,
                    "{clouds}",
                    string.Join(", ", voiceAtis).Trim(',').Trim(' '),
                    RegexOptions.IgnoreCase)
                : string.Empty;
        }

        if (textTemplate != null)
        {
            this.TextAtis = textAtis.Count > 0
                ? Regex.Replace(
                    textTemplate,
                    "{clouds}",
                    string.Join(" ", textAtis).Trim(' '),
                    RegexOptions.IgnoreCase)
                : string.Empty;
        }
    }

    private string FormatCloudsText(CloudLayer layer)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        if (this.Station.AtisFormat.Clouds.Types.TryGetValue(AmountToString(layer.Amount), out var value))
        {
            var template = value.Text;

            if (layer.Type == CloudLayer.CloudType.CannotMeasure || layer.BaseHeight == null)
            {
                template = Regex.Replace(
                    template,
                    "{altitude}",
                    $" {this.Station.AtisFormat.Clouds.UndeterminedLayerAltitude.Text} ",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                var height = (int)layer.BaseHeight.ActualValue;

                if (this.Station.AtisFormat.Clouds.ConvertToMetric)
                {
                    height *= 30;
                }
                else if (this.Station.AtisFormat.Clouds.IsAltitudeInHundreds)
                {
                    height *= 100;
                }

                // Match {altitude} or {altitude:N}, where N is the number of digits
                template = Regex.Replace(
                    template,
                    @"\{altitude(?::(\d+))?\}",
                    match =>
                    {
                        var minDigits = 3;
                        if (int.TryParse(match.Groups[1].Value, out var specifiedDigits))
                        {
                            minDigits = specifiedDigits;
                        }

                        return height.ToString(new string('0', minDigits));
                    },
                    RegexOptions.IgnoreCase);
            }

            template = layer.Type != CloudLayer.CloudType.None
                ? Regex.Replace(template, "{convective}", TypeToString(layer.Type), RegexOptions.IgnoreCase)
                : Regex.Replace(template, "{convective}", "", RegexOptions.IgnoreCase);

            return template.Trim().ToUpperInvariant();
        }

        return "";
    }

    private string FormatCloudsVoice(CloudLayer layer)
    {
        ArgumentNullException.ThrowIfNull(this.Station);

        if (this.Station.AtisFormat.Clouds.Types.TryGetValue(AmountToString(layer.Amount), out var value))
        {
            var template = value.Voice;

            if (layer.Type == CloudLayer.CloudType.CannotMeasure || layer.BaseHeight == null)
            {
                template = Regex.Replace(
                    template,
                    "{altitude}",
                    $" {this.Station.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice} ",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                var height = (int)layer.BaseHeight.ActualValue;
                height *= this.Station.AtisFormat.Clouds.ConvertToMetric ? 30 : 100;
                template = Regex.Replace(
                    template,
                    "{altitude}",
                    this.Station.AtisFormat.Clouds.ConvertToMetric
                        ? (height < 1000 ? $" {height.ToGroupForm()} " : $" {height.ToWordString()} ") + " meters "
                        : $" {height.ToWordString()} ",
                    RegexOptions.IgnoreCase);
            }

            template = Regex.Replace(
                template,
                "{convective}",
                layer.Type != CloudLayer.CloudType.None
                    ? this.Station.AtisFormat.Clouds.ConvectiveTypes.GetValueOrDefault(TypeToString(layer.Type), "")
                    : "",
                RegexOptions.IgnoreCase);

            return this.Station.AtisFormat.Clouds.IdentifyCeilingLayer && layer == this._ceilingLayer
                ? "ceiling " + template.Trim().ToUpperInvariant()
                : template.Trim().ToUpperInvariant();
        }

        return "";
    }

    private static string TypeToString(CloudLayer.CloudType type)
    {
        return type switch
        {
            CloudLayer.CloudType.Cumulonimbus => "CB",
            CloudLayer.CloudType.ToweringCumulus => "TCU",
            CloudLayer.CloudType.CannotMeasure => "///",
            CloudLayer.CloudType.None => "",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, @"Unknown CloudType")
        };
    }

    private static string AmountToString(CloudLayer.CloudAmount amount)
    {
        return amount switch
        {
            CloudLayer.CloudAmount.None => "",
            CloudLayer.CloudAmount.Few => "FEW",
            CloudLayer.CloudAmount.Broken => "BKN",
            CloudLayer.CloudAmount.Scattered => "SCT",
            CloudLayer.CloudAmount.Overcast => "OVC",
            CloudLayer.CloudAmount.VerticalVisibility => "VV",
            CloudLayer.CloudAmount.NoSignificantClouds => "NSC",
            CloudLayer.CloudAmount.NoCloudsDetected => "NCD",
            CloudLayer.CloudAmount.Clear => "CLR",
            CloudLayer.CloudAmount.SkyClear => "SKC",
            _ => throw new ArgumentOutOfRangeException(nameof(amount), amount, @"Unknown CloudAmount")
        };
    }

    public override string ParseTextVariables(CloudLayer value, string? format)
    {
        throw new NotImplementedException();
    }

    public override string ParseVoiceVariables(CloudLayer node, string? format)
    {
        throw new NotImplementedException();
    }
}