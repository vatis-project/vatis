// <copyright file="AtisFormat.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using Vatsim.Vatis.Profiles.AtisFormat.Nodes;

namespace Vatsim.Vatis.Profiles.AtisFormat;

/// <summary>
/// Represents the ATIS format with various weather and operational components.
/// </summary>
public class AtisFormat
{
    /// <summary>
    /// Gets or sets the observation time component of the ATIS format.
    /// </summary>
    public ObservationTime ObservationTime { get; set; } = new();

    /// <summary>
    /// Gets or sets the surface wind component of the ATIS format.
    /// </summary>
    public SurfaceWind SurfaceWind { get; set; } = new();

    /// <summary>
    /// Gets or sets the visibility component of the ATIS format.
    /// </summary>
    public Visibility Visibility { get; set; } = new();

    /// <summary>
    /// Gets or sets the runway visual range component of the ATIS format.
    /// </summary>
    public RunwayVisualRange RunwayVisualRange { get; set; } = new();

    /// <summary>
    /// Gets or sets the present weather component of the ATIS format.
    /// </summary>
    public PresentWeather PresentWeather { get; set; } = new();

    /// <summary>
    /// Gets or sets the recent weather component of the ATIS format.
    /// </summary>
    public RecentWeather RecentWeather { get; set; } = new();

    /// <summary>
    /// Gets or sets the clouds component of the ATIS format.
    /// </summary>
    public Clouds Clouds { get; set; } = new();

    /// <summary>
    /// Gets or sets the temperature component of the ATIS format.
    /// </summary>
    public Temperature Temperature { get; set; } = new();

    /// <summary>
    /// Gets or sets the dewpoint component of the ATIS format.
    /// </summary>
    public Dewpoint Dewpoint { get; set; } = new();

    /// <summary>
    /// Gets or sets the altimeter component of the ATIS format.
    /// </summary>
    public Altimeter Altimeter { get; set; } = new();

    /// <summary>
    /// Gets or sets the formatting for the TREND component.
    /// </summary>
    public Trend Trend { get; set; } = new();

    /// <summary>
    /// Gets or sets the transition level component of the ATIS format.
    /// </summary>
    public TransitionLevel TransitionLevel { get; set; } = new();

    /// <summary>
    /// Gets or sets the NOTAMs component of the ATIS format.
    /// </summary>
    public Notams Notams { get; set; } = new();

    /// <summary>
    /// Gets or sets the closing statement component of the ATIS format.
    /// </summary>
    public ClosingStatement ClosingStatement { get; set; } = new();

    /// <summary>
    /// Creates a new instance of <see cref="AtisFormat"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="AtisFormat"/> instance that is a copy of this instance.</returns>
    public AtisFormat Clone()
    {
        return new AtisFormat
        {
            ObservationTime = ObservationTime.Clone(),
            SurfaceWind = SurfaceWind.Clone(),
            Visibility = Visibility.Clone(),
            RunwayVisualRange = RunwayVisualRange.Clone(),
            PresentWeather = PresentWeather.Clone(),
            RecentWeather = RecentWeather.Clone(),
            Clouds = Clouds.Clone(),
            Temperature = Temperature.Clone(),
            Dewpoint = Dewpoint.Clone(),
            Altimeter = Altimeter.Clone(),
            TransitionLevel = TransitionLevel.Clone(),
            Notams = Notams.Clone(),
            ClosingStatement = ClosingStatement.Clone(),
        };
    }
}
