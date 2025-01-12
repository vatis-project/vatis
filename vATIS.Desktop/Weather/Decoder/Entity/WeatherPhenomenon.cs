// <copyright file="WeatherPhenomenon.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents a weather phenomenon with its intensity, proximity, characteristics, types, and raw value.
/// </summary>
/// <seealso cref="DecodedMetar"/>
public sealed class WeatherPhenomenon
{
    private readonly List<string> _types = [];

    /// <summary>
    /// Gets or sets the intensity and proximity of the weather phenomenon.
    /// </summary>
    public string? IntensityProximity { get; set; }

    /// <summary>
    /// Gets or sets the characteristics of the weather phenomenon.
    /// </summary>
    public string? Characteristics { get; set; }

    /// <summary>
    /// Gets the types of the weather phenomenon.
    /// </summary>
    public ReadOnlyCollection<string> Types => new(_types);

    /// <summary>
    /// Gets or sets the raw METAR value associated with the weather phenomenon.
    /// </summary>
    public string? RawValue { get; set; }

    /// <summary>
    /// Adds a new type to the weather phenomenon.
    /// </summary>
    /// <param name="type">The type describing a weather phenomenon.</param>
    public void AddType(string type)
    {
        _types.Add(type);
    }
}
