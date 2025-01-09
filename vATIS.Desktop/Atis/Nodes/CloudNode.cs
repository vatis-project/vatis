using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;
public class CloudNode : BaseNode<CloudLayer>
{
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.Clouds, metar.Ceiling);
    }

    private void Parse(List<CloudLayer> cloudLayers, CloudLayer? ceiling)
    {
        ArgumentNullException.ThrowIfNull(Station);

        var voiceAtis = new List<string>();
        var textAtis = new List<string>();

        foreach (var layer in cloudLayers)
        {
            voiceAtis.Add(FormatCloudsVoice(layer, ceiling));
            textAtis.Add(FormatCloudsText(layer));
        }

        var voiceTemplate = Station.AtisFormat.Clouds.Template.Voice;
        var textTemplate = Station.AtisFormat.Clouds.Template.Text;

        if (voiceTemplate != null)
        {
            VoiceAtis = voiceAtis.Count > 0
                ? Regex.Replace(voiceTemplate, "{clouds}", string.Join(", ", voiceAtis).Trim(',').Trim(' '),
                    RegexOptions.IgnoreCase)
                : string.Empty;
        }

        if (textTemplate != null)
        {
            TextAtis = textAtis.Count > 0
                ? Regex.Replace(textTemplate, "{clouds}", string.Join(" ", textAtis).Trim(' '),
                    RegexOptions.IgnoreCase)
                : string.Empty;
        }
    }

    private string FormatCloudsText(CloudLayer layer)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (Station.AtisFormat.Clouds.Types.TryGetValue(AmountToString(layer.Amount), out var value))
        {
            var template = value.Text;

            if (layer.Type == CloudLayer.CloudType.CannotMeasure || layer.BaseHeight == null)
            {
                template = Regex.Replace(template, "{altitude}",
                    $" {Station.AtisFormat.Clouds.UndeterminedLayerAltitude.Text} ",
                    RegexOptions.IgnoreCase);
            }
            else
            {
                var height = (int)layer.BaseHeight.ActualValue;

                if (Station.AtisFormat.Clouds.ConvertToMetric)
                    height *= 30;
                else if (Station.AtisFormat.Clouds.IsAltitudeInHundreds)
                    height *= 100;

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
                    RegexOptions.IgnoreCase
                );
            }

            template = layer.Type != CloudLayer.CloudType.None
                ? Regex.Replace(template, "{convective}", TypeToString(layer.Type), RegexOptions.IgnoreCase)
                : Regex.Replace(template, "{convective}", "", RegexOptions.IgnoreCase);

            return template.Trim().ToUpperInvariant();
        }

        return "";
    }

    private string FormatCloudsVoice(CloudLayer layer, CloudLayer? ceiling)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (Station.AtisFormat.Clouds.Types.TryGetValue(AmountToString(layer.Amount), out var value))
        {
            var template = value.Voice;

            if (layer.Type == CloudLayer.CloudType.CannotMeasure || layer.BaseHeight == null)
            {
                template = Regex.Replace(template, "{altitude}",
                    $" {Station.AtisFormat.Clouds.UndeterminedLayerAltitude.Voice} ", RegexOptions.IgnoreCase);
            }
            else
            {
                var height = (int)layer.BaseHeight.ActualValue;
                height *= Station.AtisFormat.Clouds.ConvertToMetric ? 30 : 100;
                template = Regex.Replace(template, "{altitude}",
                    Station.AtisFormat.Clouds.ConvertToMetric
                        ? (height < 1000 ? $" {height.ToGroupForm()} " : $" {height.ToWordString()} ") + " meters "
                        : $" {height.ToWordString()} ", RegexOptions.IgnoreCase);
            }

            template = Regex.Replace(template, "{convective}",
                layer.Type != CloudLayer.CloudType.None
                    ? Station.AtisFormat.Clouds.ConvectiveTypes.GetValueOrDefault(TypeToString(layer.Type), "")
                    : "", RegexOptions.IgnoreCase);

            return Station.AtisFormat.Clouds.IdentifyCeilingLayer && layer == ceiling
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
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown CloudType")
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
            _ => throw new ArgumentOutOfRangeException(nameof(amount), amount, "Unknown CloudAmount")
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