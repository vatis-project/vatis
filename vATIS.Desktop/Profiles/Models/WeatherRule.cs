// <copyright file="WeatherRule.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// The METAR value a <see cref="WeatherRule"/> is evaluated against.
/// </summary>
public enum WeatherField
{
    /// <summary>
    /// The ceiling height.
    /// </summary>
    Ceiling,

    /// <summary>
    /// The lowest reported runway visual range.
    /// </summary>
    Rvr,

    /// <summary>
    /// The prevailing visibility.
    /// </summary>
    Visibility,

    /// <summary>
    /// The lowest cloud base of any layer, whatever its coverage.
    /// </summary>
    CloudBase,

    /// <summary>
    /// The mean wind speed.
    /// </summary>
    WindSpeed,

    /// <summary>
    /// The wind gust speed.
    /// </summary>
    WindGust,

    /// <summary>
    /// The mean wind direction. Not met for variable winds.
    /// </summary>
    WindDirection,

    /// <summary>
    /// The air temperature.
    /// </summary>
    Temperature,

    /// <summary>
    /// The dewpoint.
    /// </summary>
    Dewpoint,

    /// <summary>
    /// The QNH / altimeter setting.
    /// </summary>
    Pressure,

    /// <summary>
    /// The deepest reported runway deposit.
    /// </summary>
    RunwayDepth,

    /// <summary>
    /// The present weather codes, e.g. TS, FZRA or +SN.
    /// </summary>
    PresentWeather,

    /// <summary>
    /// The cloud types, CB or TCU.
    /// </summary>
    CloudType,

    /// <summary>
    /// Whether windshear is reported.
    /// </summary>
    Windshear,

    /// <summary>
    /// Whether the METAR reports CAVOK.
    /// </summary>
    Cavok,
}

/// <summary>
/// How a <see cref="WeatherRule"/> compares the METAR value to its threshold.
/// </summary>
public enum WeatherOperator
{
    /// <summary>
    /// The value is below the threshold.
    /// </summary>
    Below,

    /// <summary>
    /// The value is above the threshold.
    /// </summary>
    Above,

    /// <summary>
    /// The value is at or below the threshold.
    /// </summary>
    AtOrBelow,

    /// <summary>
    /// The value is at or above the threshold.
    /// </summary>
    AtOrAbove,

    /// <summary>
    /// The value equals the threshold.
    /// </summary>
    EqualTo,

    /// <summary>
    /// The METAR includes the text, or reports the condition.
    /// </summary>
    Contains,

    /// <summary>
    /// The METAR does not include the text, or does not report the condition.
    /// </summary>
    NotContains,
}

/// <summary>
/// The unit of a <see cref="WeatherRule"/> threshold.
/// </summary>
public enum WeatherUnit
{
    /// <summary>
    /// Feet.
    /// </summary>
    Feet,

    /// <summary>
    /// Meters.
    /// </summary>
    Meters,

    /// <summary>
    /// Kilometers.
    /// </summary>
    Kilometers,

    /// <summary>
    /// Knots.
    /// </summary>
    Knots,

    /// <summary>
    /// Kilometers per hour.
    /// </summary>
    KilometersPerHour,

    /// <summary>
    /// Meters per second.
    /// </summary>
    MetersPerSecond,

    /// <summary>
    /// Degrees of heading.
    /// </summary>
    Degrees,

    /// <summary>
    /// Degrees Celsius.
    /// </summary>
    Celsius,

    /// <summary>
    /// Degrees Fahrenheit.
    /// </summary>
    Fahrenheit,

    /// <summary>
    /// Hectopascals.
    /// </summary>
    HectoPascals,

    /// <summary>
    /// Inches of mercury.
    /// </summary>
    InchesOfMercury,

    /// <summary>
    /// Millimeters.
    /// </summary>
    Millimeters,

    /// <summary>
    /// Statute miles.
    /// </summary>
    StatuteMiles,
}

/// <summary>
/// A single comparison parsed from a weather condition, e.g. <c>ceiling &lt; 200ft</c>.
/// </summary>
public class WeatherRule
{
    /// <summary>
    /// Gets or sets the METAR value to compare.
    /// </summary>
    public WeatherField Field { get; set; }

    /// <summary>
    /// Gets or sets the comparison.
    /// </summary>
    public WeatherOperator Operator { get; set; }

    /// <summary>
    /// Gets or sets the threshold.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the code or codes (separated by commas or spaces) for text fields such as present weather.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the threshold unit.
    /// </summary>
    public WeatherUnit Unit { get; set; } = WeatherUnit.Meters;
}
