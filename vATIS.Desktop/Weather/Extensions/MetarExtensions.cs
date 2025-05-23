// <copyright file="MetarExtensions.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Weather.Extensions;

/// <summary>
/// Provides extension methods for processing METAR-related data.
/// </summary>
public static class MetarExtensions
{
    private static readonly Dictionary<string, CloudLayer.CloudAmount> s_typeMapping =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SCT"] = CloudLayer.CloudAmount.Scattered,
            ["BKN"] = CloudLayer.CloudAmount.Broken,
            ["OVC"] = CloudLayer.CloudAmount.Overcast
        };

    /// <summary>
    /// Retrieves the ceiling cloud layer from a list of cloud layers based on the specified ATIS format.
    /// </summary>
    /// <param name="atisFormat">The ATIS format containing cloud layer type preferences.</param>
    /// <param name="cloudLayers">A list of cloud layers to evaluate.</param>
    /// <returns>
    /// The lowest cloud layer that qualifies as a ceiling or <c>null</c> if none is found.
    /// </returns>
    public static CloudLayer? GetCeilingLayer(this AtisFormat atisFormat, List<CloudLayer>? cloudLayers)
    {
        if (cloudLayers == null)
            return null;

        var ceilingTypes = new List<CloudLayer.CloudAmount>();

        if (atisFormat.Clouds.CloudCeilingLayerTypes.Count == 0)
        {
            // Default to BKN and OVC
            ceilingTypes.Add(CloudLayer.CloudAmount.Broken);
            ceilingTypes.Add(CloudLayer.CloudAmount.Overcast);
        }
        else
        {
            foreach (var type in atisFormat.Clouds.CloudCeilingLayerTypes)
            {
                if (s_typeMapping.TryGetValue(type, out var cloudAmount))
                {
                    ceilingTypes.Add(cloudAmount);
                }
            }
        }

        return cloudLayers
            .Where(layer => ceilingTypes.Contains(layer.Amount) && layer.BaseHeight is { ActualValue: > 0 })
            .OrderBy(x => x.BaseHeight?.ActualValue).FirstOrDefault();
    }
}
