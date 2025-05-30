// <copyright file="RunwayVisualRange.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents the runway visual range (RVR), which provides information about the visibility
/// or range of vision along a specific runway, including changes, variability, and raw data.
/// </summary>
public sealed class RunwayVisualRange
{
    /// <summary>
    /// Past tendency (optional) (U, D, or N).
    /// </summary>
    public enum Tendency
    {
        /// <summary>
        /// Represents the absence of a past tendency in runway visual range data.
        /// </summary>
        None,

        /// <summary>
        /// Indicates an upward tendency in runway visual range data.
        /// </summary>
        U,

        /// <summary>
        /// Indicates a decreasing tendency in runway visual range data.
        /// </summary>
        D,

        /// <summary>
        /// Represents a "no change" tendency in runway visual range data.
        /// </summary>
        N
    }

    /// <summary>
    /// Gets or sets the runway identifier associated with the visual range.
    /// </summary>
    public string? Runway { get; set; }

    /// <summary>
    /// Gets or sets the runway identifier suffix (L/R/C).
    /// </summary>
    public string? RunwaySuffix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the RVR is reported as missing (////).
    /// </summary>
    public bool IsMissing { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the reported value is "less than" (e.g., M0600).
    /// </summary>
    public bool IsLessThan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the reported value is "greater than" (e.g., P1500).
    /// </summary>
    public bool IsGreaterThan { get; set; }

    /// <summary>
    /// Gets or sets the visual range value associated with the runway.
    /// </summary>
    public Value? VisualRange { get; set; }

    /// <summary>
    /// Gets or sets the range intervals representing the minimum and maximum visual ranges on the runway.
    /// </summary>
    public Value[]? VisualRangeInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the visual range for the runway is variable.
    /// </summary>
    public bool Variable { get; set; }

    /// <summary>
    /// Gets or sets the past tendency, which represents changes in the visual range
    /// over time (optional) (U for Up, D for Down, or N for No change).
    /// </summary>
    public Tendency PastTendency { get; set; }

    /// <summary>
    /// Gets or sets the raw string representation of the runway visual range data.
    /// </summary>
    public string? RawValue { get; set; }
}
