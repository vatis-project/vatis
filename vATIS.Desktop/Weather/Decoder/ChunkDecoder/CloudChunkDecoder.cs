// <copyright file="CloudChunkDecoder.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Weather.Decoder.ChunkDecoder.Abstract;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Decoder.Exception;

namespace Vatsim.Vatis.Weather.Decoder.ChunkDecoder;

/// <summary>
/// Decodes cloud-related information from METAR weather reports.
/// </summary>
/// <remarks>
/// The <see cref="CloudChunkDecoder"/> class is responsible for interpreting and extracting cloud-specific data elements from a METAR weather observation, including cloud layers, coverage, altitude, and related parameters.
/// </remarks>
public sealed class CloudChunkDecoder : MetarChunkDecoder
{
    private const string CeilingParameterName = "Ceiling";
    private const string CloudsParameterName = "Clouds";

    /// <summary>
    /// No cloud regex.
    /// </summary>
    public const string NoCloudRegexPattern = "(NSC|NCD|CLR|SKC)";

    /// <summary>
    /// Cloud layer regex.
    /// </summary>
    public const string LayerRegexPattern = "(VV|FEW|SCT|BKN|OVC|///)([0-9]{3}|///)(CB|TCU|///)?";

    /// <summary>
    /// Retrieves the regular expression pattern associated with the <see cref="CloudChunkDecoder"/> class.
    /// </summary>
    /// <returns>A string representing the regular expression pattern for decoding cloud-related METAR data.</returns>
    public override string GetRegex()
    {
        return
            $"^({NoCloudRegexPattern}|({LayerRegexPattern})( {LayerRegexPattern})?( {LayerRegexPattern})?( {LayerRegexPattern})?)( )";
    }

    /// <summary>
    /// Parses the remaining METAR data and extracts cloud information using the <see cref="CloudChunkDecoder"/> class.
    /// </summary>
    /// <param name="remainingMetar">The remaining METAR string to be parsed for cloud information.</param>
    /// <param name="withCavok">A boolean indicating whether to interpret "CAVOK" as a valid cloud condition.</param>
    /// <returns>A dictionary containing parsed cloud-related data, structured as key-value pairs.</returns>
    /// <exception cref="MetarChunkDecoderException">Thrown when the cloud information in the METAR data has an invalid format.</exception>
    public override Dictionary<string, object> Parse(string remainingMetar, bool withCavok = false)
    {
        var consumed = Consume(remainingMetar);
        var found = consumed.Value;
        var newRemainingMetar = consumed.Key;
        var result = new Dictionary<string, object?>();
        var layers = new List<CloudLayer>();

        if (found.Count <= 1 && !withCavok)
        {
            throw new MetarChunkDecoderException(
                remainingMetar,
                newRemainingMetar,
                MetarChunkDecoderException.Messages.CloudsInformationBadFormat);
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
                _ => layer.Amount,
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
                        _ => layer.Amount,
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
                        _ => layer.Type,
                    };

                    layers.Add(layer);
                }
            }
        }

        result.Add(CeilingParameterName, CalculateCeiling(layers));
        result.Add(CloudsParameterName, layers);
        return GetResults(newRemainingMetar, result);
    }

    /// <summary>
    /// Calculates the ceiling by finding the lowest broken, overcast, or vertical visibility cloud layer.
    /// </summary>
    /// <param name="layers">The list of cloud layers to analyze.</param>
    /// <returns>The lowest broken, overcast, or vertical visibility cloud layer with height > 0 feet, or null if no such layer exists.</returns>
    private static CloudLayer? CalculateCeiling(List<CloudLayer> layers)
    {
        var ceiling = layers
            .Where(layer =>
                    layer.BaseHeight != null &&
                    layer.BaseHeight.ActualValue > 0 &&
                    (layer.Amount == CloudLayer.CloudAmount.VerticalVisibility ||
                     layer.Amount == CloudLayer.CloudAmount.Overcast ||
                     layer.Amount == CloudLayer.CloudAmount.Broken))
            .OrderBy(layer => layer.BaseHeight?.ActualValue)
            .FirstOrDefault();

        return ceiling;
    }
}
