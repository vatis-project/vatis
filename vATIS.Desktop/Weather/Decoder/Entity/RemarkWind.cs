// <copyright file="RemarkWind.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents a wind report found in the METAR remarks, e.g. <c>RMK WIND 1200FT 29003KT</c>.
/// </summary>
public class RemarkWind
{
    /// <summary>
    /// Gets or sets the height of the wind observation in feet.
    /// </summary>
    public int HeightFeet { get; set; }

    /// <summary>
    /// Gets or sets the wind direction in degrees, or null if variable.
    /// </summary>
    public int? Direction { get; set; }

    /// <summary>
    /// Gets or sets the wind speed in knots.
    /// </summary>
    public int Speed { get; set; }

    /// <summary>
    /// Gets or sets the gust speed in knots, if reported.
    /// </summary>
    public int? Gust { get; set; }
}
