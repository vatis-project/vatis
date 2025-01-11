// <copyright file="RunwayVisualRange.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

public sealed class RunwayVisualRange
{
    /// <summary>
    /// Past tendency (optional) (U, D, or N)
    /// </summary>
    public enum Tendency
    {
        None,
        U,
        D,
        N,
    }

    /// <summary>
    /// Concerned runway
    /// </summary>
    public string? Runway { get; set; }

    /// <summary>
    /// Visual range defined by one value
    /// </summary>
    public Value? VisualRange { get; set; }

    /// <summary>
    /// Visual range defined by an interval (because it is variable)
    /// </summary>
    public Value[]? VisualRangeInterval { get; set; }

    /// <summary>
    /// Is it a variable range ?
    /// </summary>
    public bool Variable { get; set; }

    /// <summary>
    /// Past tendency (optional) (U, D, or N)
    /// </summary>
    public Tendency PastTendency { get; set; }
    
    /// <summary>
    /// The raw string value
    /// </summary>
    public string? RawValue { get; set; }
}