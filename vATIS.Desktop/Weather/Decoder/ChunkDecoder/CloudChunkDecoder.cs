using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.OpenGL.Egl;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

public sealed class CloudChunkDecoder : MetarChunkDecoder
{
    private const string CeilingParameterName = "Ceiling";
    private const string CloudsParameterName = "Clouds";
    private const string NoCloudRegexPattern = "(NSC|NCD|CLR|SKC)";
    private const string LayerRegexPattern = "(VV|FEW|SCT|BKN|OVC|///)([0-9]{3}|///)(CB|TCU|///)?";

    public override string GetRegex()
    {
        return $"^({NoCloudRegexPattern}|({LayerRegexPattern})( {LayerRegexPattern})?( {LayerRegexPattern})?( {LayerRegexPattern})?)( )";
    }

    private static CloudLayer? CalculateCeiling(List<CloudLayer> layers)
    {
        var ceiling = layers
            .Where(n => n.BaseHeight?.ActualValue > 0 &&
                        n.Amount is CloudLayer.CloudAmount.Overcast or CloudLayer.CloudAmount.Broken)
            .Select(n => n)
            .OrderBy(n => n.BaseHeight?.ActualValue)
            .FirstOrDefault();

        return ceiling;
    }

    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();
        var layers = new List<CloudLayer>();

        if (found.Count <= 1 && !withCavok)
        {
            throw new MetarChunkDecoderException(remainingMetar, newRemainingMetar,
                MetarChunkDecoderException.Messages.CLOUDS_INFORMATION_BAD_FORMAT);
        }

        if (Regex.IsMatch(found[0].Value, NoCloudRegexPattern))
        {
            var layer = new CloudLayer();
            layer.Amount = found[0].Value.Trim() switch
            {
                "NSC" => CloudLayer.CloudAmount.NoSignificantClouds,
                "NCD" => CloudLayer.CloudAmount.NoCloudsDetected,
                "CLR" => CloudLayer.CloudAmount.Clear,
                "SKC" => CloudLayer.CloudAmount.SkyClear,
                _ => layer.Amount
            };
            layers.Add(layer);
        }
        // there are clouds, handle cloud layers and visibility
        else if (found.Count > 2 && string.IsNullOrEmpty(found[2].Value))
        {
            for (var i = 3; i <= 15; i += 4)
            {
                if (!string.IsNullOrEmpty(found[i].Value))
                {
                    var layer = new CloudLayer();
                    var layerHeight = Value.ToInt(found[i + 2].Value);

                    layer.Amount = found[i + 1].Value switch
                    {
                        "FEW" => CloudLayer.CloudAmount.Few,
                        "SCT" => CloudLayer.CloudAmount.Scattered,
                        "BKN" => CloudLayer.CloudAmount.Broken,
                        "OVC" => CloudLayer.CloudAmount.Overcast,
                        "VV" => CloudLayer.CloudAmount.VerticalVisibility,
                        _ => layer.Amount
                    };

                    if (layerHeight.HasValue)
                    {
                        layer.BaseHeight = new Value(layerHeight.Value, Value.Unit.Feet);
                    }

                    layer.Type = found[i + 3].Value switch
                    {
                        "CB" => CloudLayer.CloudType.Cumulonimbus,
                        "TCU" => CloudLayer.CloudType.ToweringCumulus,
                        "///" => CloudLayer.CloudType.CannotMeasure,
                        _ => layer.Type
                    };

                    layers.Add(layer);
                }
            }
        }

        result.Add(CeilingParameterName, CalculateCeiling(layers));
        result.Add(CloudsParameterName, layers);
        return GetResults(newRemainingMetar, result);
    }
}