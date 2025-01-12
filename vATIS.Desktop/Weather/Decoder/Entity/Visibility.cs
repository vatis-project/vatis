// <copyright file="Visibility.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents visibility-related information decoded from METAR reports.
/// </summary>
public sealed class Visibility
{
    /// <summary>
    /// Gets or sets the prevailing visibility.
    /// </summary>
    public Value? PrevailingVisibility { get; set; }

    /// <summary>
    /// Gets or sets the minimum visibility.
    /// </summary>
    public Value? MinimumVisibility { get; set; }

    /// <summary>
    /// Gets or sets the direction of the minimum visibility.
    /// </summary>
    public string? MinimumVisibilityDirection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether no directional variation (NDV) is reported in the visibility data.
    /// </summary>
    public bool IsNdv { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the weather observation includes CAVOK
    /// (Ceiling And Visibility OK) conditions.
    /// </summary>
    public bool IsCavok { get; set; }

    /// <summary>
    /// Gets or sets the raw value of the visibility as retrieved from the METAR report.
    /// </summary>
    public string? RawValue { get; set; }
}
