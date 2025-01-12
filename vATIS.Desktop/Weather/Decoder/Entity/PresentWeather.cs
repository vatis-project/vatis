// <copyright file="PresentWeather.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents the weather conditions, including precipitation, obscurations, and vicinities.
/// </summary>
public sealed class PresentWeather
{
    private readonly List<int> _obscurations = [];
    private readonly List<int> _precipitations = [];
    private readonly List<int> _vicinities = [];

    /// <summary>
    /// Gets precipitations phenomenon.
    /// </summary>
    public ReadOnlyCollection<int> Precipitations => new(_precipitations);

    /// <summary>
    /// Gets obscurations phenomenon.
    /// </summary>
    public ReadOnlyCollection<int> Obscurations => new(_obscurations);

    /// <summary>
    /// Gets vicinities phenomenon.
    /// </summary>
    public ReadOnlyCollection<int> Vicinities => new(_vicinities);

    /// <summary>
    /// Add Precipitation.
    /// </summary>
    /// <param name="precipitation">The precipitation to be added.</param>
    public void AddPrecipitation(int precipitation)
    {
        _precipitations.Add(precipitation);
    }

    /// <summary>
    /// Add Obscuration.
    /// </summary>
    /// <param name="obscurationPhenomenon">The obscuration phenomenon to be added.</param>
    public void AddObscuration(int obscurationPhenomenon)
    {
        _obscurations.Add(obscurationPhenomenon);
    }

    /// <summary>
    /// Add Vicinity.
    /// </summary>
    /// <param name="vicinity">The vicinity to be added.</param>
    public void AddVicinity(int vicinity)
    {
        _vicinities.Add(vicinity);
    }
}
