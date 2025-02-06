// <copyright file="TrendForecast.cs" company="Afonso Dutra Nogueira Filho">
// Copyright (c) Afonso Dutra Nogueira Filho. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// https://github.com/afonsoft/metar-decoder
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Defines the types of trend change indicators in a METAR report.
/// </summary>
public enum TrendForecastType
{
    /// <summary>
    /// Represents the absence of a trend change indicator in a METAR report.
    /// </summary>
    /// <remarks>
    /// This value indicates that no specific trend forecast information is provided in the report.
    /// It implies that the weather conditions are expected to remain as reported without significant changes.
    /// </remarks>
    /// <seealso cref="TrendForecastType"/>
    None,

    /// <summary>
    /// Represents a forecast trend indicating gradual changes in weather conditions.
    /// </summary>
    /// <remarks>
    /// This value signifies that the weather conditions are expected to transition slowly
    /// from their current state to a new state during the forecast period in a METAR report.
    /// </remarks>
    /// <seealso cref="TrendForecastType"/>
    Becoming,

    /// <summary>
    /// Represents a forecast trend indicating temporary fluctuations in weather conditions.
    /// </summary>
    /// <remarks>
    /// This value signifies that the weather conditions are expected to deviate temporarily
    /// from their current state but will return to the original conditions within the forecast period
    /// outlined in the METAR report.
    /// </remarks>
    /// <seealso cref="TrendForecastType"/>
    Temporary,

    /// <summary>
    /// Indicates that no significant changes in weather conditions are expected.
    /// </summary>
    /// <remarks>
    /// This value is used in a METAR report to specify that the reported weather conditions
    /// are not expected to undergo notable variations during the forecast period.
    /// </remarks>
    /// <seealso cref="TrendForecastType"/>
    NoSignificantChanges
}

/// <summary>
/// Represents the forecast trend information in a METAR report.
/// </summary>
public sealed class TrendForecast
{
    /// <summary>
    /// Gets or sets the type of trend change indicator for a METAR report.
    /// </summary>
    /// <remarks>
    /// This property indicates the nature of the forecast trend.
    /// Possible values are defined in the <see cref="TrendForecastType"/> enumeration.
    /// </remarks>
    public TrendForecastType ChangeIndicator { get; set; } = TrendForecastType.None;

    /// <summary>
    /// Gets or sets the "from time" information for a forecast trend in a METAR report.
    /// </summary>
    /// <remarks>
    /// This property represents the starting time for the forecast trend.
    /// The value is typically expressed in a specific time format used in METAR reports.
    /// </remarks>
    public string? FromTime { get; set; }

    /// <summary>
    /// Gets or sets the time until which the trend forecast is valid.
    /// </summary>
    /// <remarks>
    /// This property represents the end time for the forecast trend, typically derived from METAR data.
    /// The value is expected to be in a specific time format depending on the METAR specification.
    /// </remarks>
    public string? UntilTime { get; set; }

    /// <summary>
    /// Gets or sets the specific time associated with the weather trend forecast in a METAR report.
    /// </summary>
    /// <remarks>
    /// This property represents the exact time (e.g., "AT") as outlined in the trend forecast section of the METAR.
    /// It is commonly used to indicate when a specific significant weather event is expected to occur.
    /// </remarks>
    public string? AtTime { get; set; }

    /// <summary>
    /// Gets or sets the forecast details associated with a METAR trend.
    /// </summary>
    /// <remarks>
    /// This property contains the specific forecast information parsed from the METAR trend data.
    /// </remarks>
    public string? Forecast { get; set; }

    /// <summary>
    /// Gets or sets the surface wind from the TREND forecast.
    /// </summary>
    public string? SurfaceWind { get; set; }

    /// <summary>
    /// Gets or sets the TREND prevailing visibility value.
    /// </summary>
    public string? PrevailingVisibility { get; set; }

    /// <summary>
    /// Gets or sets the recent weather value.
    /// </summary>
    public string? WeatherCodes { get; set; }

    /// <summary>
    /// Gets or sets the cloud layer values.
    /// </summary>
    public string? Clouds { get; set; }
}
