// <copyright file="CloudLayer.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

public class CloudLayer
{
    /// <summary>
    /// Annotation corresponding to amount of clouds (FEW/SCT/BKN/OVC)
    /// </summary>
    public enum CloudAmount
    {
        None,
        Few,
        Scattered,
        Broken,
        Overcast,
        VerticalVisibility,
        NoSignificantClouds,
        NoCloudsDetected,
        Clear,
        SkyClear 
    }

    /// <summary>
    /// Cloud type cumulonimbus, towering cumulonimbus (CB/TCU)
    /// </summary>
    public enum CloudType
    {
        None,
        Cumulonimbus,
        ToweringCumulus, 
        CannotMeasure,
    }

    /// <summary>
    /// Annotation corresponding to amount of clouds (FEW/SCT/BKN/OVC)
    /// </summary>
    public CloudAmount Amount { get; set; } = CloudAmount.None;

    /// <summary>
    /// Height of cloud base
    /// </summary>
    public Value? BaseHeight { get; set; }

    /// <summary>
    /// Cloud type cumulonimbus, towering cumulonimbus (CB/TCU)
    /// </summary>
    public CloudType Type { get; set; } = CloudType.None;
}