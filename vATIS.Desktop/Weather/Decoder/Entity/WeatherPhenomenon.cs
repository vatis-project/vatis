// <copyright file="WeatherPhenomenon.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// WeatherPhenomenon
/// </summary>
public sealed class WeatherPhenomenon
{
    private readonly List<string> mTypes = [];

    /// <summary>
    /// Intensity/proximity of the phenomenon + / - / VC (=vicinity)
    /// </summary>
    public string? IntensityProximity { get; set; }

    /// <summary>
    /// Characteristics of the phenomenon
    /// </summary>
    public string? Characteristics { get; set; }

    /// <summary>
    /// Types of phenomenon
    /// </summary>
    public ReadOnlyCollection<string> Types => new(mTypes);

    /// <summary>
    /// Raw string value
    /// </summary>
    public string? RawValue { get; set; }

    /// <summary>
    /// AddType
    /// </summary>
    /// <param name="type">type</param>
    public void AddType(string type)
    {
        mTypes.Add(type);
    }
}