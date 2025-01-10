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
            ObservationTime = this.ObservationTime.Clone(),
            SurfaceWind = this.SurfaceWind.Clone(),
            Visibility = this.Visibility.Clone(),
            PresentWeather = this.PresentWeather.Clone(),
            RecentWeather = this.RecentWeather.Clone(),
            Clouds = this.Clouds.Clone(),
            Temperature = this.Temperature.Clone(),
            Dewpoint = this.Dewpoint.Clone(),
            Altimeter = this.Altimeter.Clone(),
            TransitionLevel = this.TransitionLevel.Clone(),
            Notams = this.Notams.Clone(),
            ClosingStatement = this.ClosingStatement.Clone(),
        };
    }
}
