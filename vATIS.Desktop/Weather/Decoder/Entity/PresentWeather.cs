// <copyright file="PresentWeather.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents the weather conditions, including precipitation, obscurations, and vicinities.
/// </summary>
public sealed class PresentWeather
{
    private readonly List<int> obscurations = [];
    private readonly List<int> precipitations = [];
    private readonly List<int> vicinities = [];

    /// <summary>
    /// Gets precipitations phenomenon.
    /// </summary>
    public ReadOnlyCollection<int> Precipitations => new(this.precipitations);

    /// <summary>
    /// Gets obscurations phenomenon.
    /// </summary>
    public ReadOnlyCollection<int> Obscurations => new(this.obscurations);

    /// <summary>
    /// Gets vicinities phenomenon.
    /// </summary>
    public ReadOnlyCollection<int> Vicinities => new(this.vicinities);

    /// <summary>
    /// Add Precipitation.
    /// </summary>
    /// <param name="precipitation">The precipitation to be added.</param>
    public void AddPrecipitation(int precipitation)
    {
        this.precipitations.Add(precipitation);
    }

    /// <summary>
    /// Add Obscuration.
    /// </summary>
    /// <param name="obscurationPhenomenon">The obscuration phenomenon to be added.</param>
    public void AddObscuration(int obscurationPhenomenon)
    {
        this.obscurations.Add(obscurationPhenomenon);
    }

    /// <summary>
    /// Add Vicinity.
    /// </summary>
    /// <param name="vicinity">The vicinity to be added.</param>
    public void AddVicinity(int vicinity)
    {
        this.vicinities.Add(vicinity);
    }
}
