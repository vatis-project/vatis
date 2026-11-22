// <copyright file="WeatherExpression.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// The weather fields, aliases and units that can be used in a weather term of a <see cref="ConditionExpression"/>,
/// e.g. <c>ceiling &lt; 200ft</c>.
/// </summary>
public static partial class WeatherExpression
{
    private static readonly Dictionary<string, WeatherField> s_fields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ceiling"] = WeatherField.Ceiling,
        ["cloudbase"] = WeatherField.CloudBase,
        ["rvr"] = WeatherField.Rvr,
        ["visibility"] = WeatherField.Visibility,
        ["vis"] = WeatherField.Visibility,
        ["windspeed"] = WeatherField.WindSpeed,
        ["wind"] = WeatherField.WindSpeed,
        ["gust"] = WeatherField.WindGust,
        ["windgust"] = WeatherField.WindGust,
        ["winddirection"] = WeatherField.WindDirection,
        ["winddir"] = WeatherField.WindDirection,
        ["temperature"] = WeatherField.Temperature,
        ["temp"] = WeatherField.Temperature,
        ["dewpoint"] = WeatherField.Dewpoint,
        ["dew"] = WeatherField.Dewpoint,
        ["pressure"] = WeatherField.Pressure,
        ["qnh"] = WeatherField.Pressure,
        ["altimeter"] = WeatherField.Pressure,
        ["depth"] = WeatherField.RunwayDepth,
        ["runwaydepth"] = WeatherField.RunwayDepth,
        ["weather"] = WeatherField.PresentWeather,
        ["wx"] = WeatherField.PresentWeather,
        ["recentweather"] = WeatherField.RecentWeather,
        ["recentwx"] = WeatherField.RecentWeather,
        ["trendtype"] = WeatherField.TrendType,
        ["cloudtype"] = WeatherField.CloudType,
        ["windshear"] = WeatherField.Windshear,
        ["ws"] = WeatherField.Windshear,
        ["cavok"] = WeatherField.Cavok,
    };

    private static readonly (string Prefix, TrendScope Scope)[] s_trendPrefixes =
    [
        ("trend", TrendScope.Any),
        ("tempo", TrendScope.Tempo),
        ("becmg", TrendScope.Becoming),
    ];

    // The values a trend forecast can carry, which can be read by prefixing the field with "trend", "tempo" or
    // "becmg".
    private static readonly HashSet<WeatherField> s_trendFields =
    [
        WeatherField.Ceiling,
        WeatherField.CloudBase,
        WeatherField.Visibility,
        WeatherField.WindSpeed,
        WeatherField.WindGust,
        WeatherField.WindDirection,
        WeatherField.PresentWeather,
        WeatherField.CloudType,
        WeatherField.Cavok,
    ];

    internal static readonly Dictionary<string, WeatherUnit> s_units = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ft"] = WeatherUnit.Feet,
        ["feet"] = WeatherUnit.Feet,
        ["m"] = WeatherUnit.Meters,
        ["meters"] = WeatherUnit.Meters,
        ["metres"] = WeatherUnit.Meters,
        ["km"] = WeatherUnit.Kilometers,
        ["sm"] = WeatherUnit.StatuteMiles,
        ["mi"] = WeatherUnit.StatuteMiles,
        ["miles"] = WeatherUnit.StatuteMiles,
        ["kt"] = WeatherUnit.Knots,
        ["kts"] = WeatherUnit.Knots,
        ["knots"] = WeatherUnit.Knots,
        ["kmh"] = WeatherUnit.KilometersPerHour,
        ["km/h"] = WeatherUnit.KilometersPerHour,
        ["kph"] = WeatherUnit.KilometersPerHour,
        ["mps"] = WeatherUnit.MetersPerSecond,
        ["m/s"] = WeatherUnit.MetersPerSecond,
        ["deg"] = WeatherUnit.Degrees,
        ["degrees"] = WeatherUnit.Degrees,
        ["c"] = WeatherUnit.Celsius,
        ["f"] = WeatherUnit.Fahrenheit,
        ["hpa"] = WeatherUnit.HectoPascals,
        ["mb"] = WeatherUnit.HectoPascals,
        ["inhg"] = WeatherUnit.InchesOfMercury,
        ["mm"] = WeatherUnit.Millimeters,
    };

    /// <summary>
    /// Gets a short description of the syntax, for tooltips and error messages.
    /// </summary>
    public static string FieldList =>
        "ceiling, cloudbase, rvr, visibility, windspeed, gust, winddir, temperature, dewpoint, qnh, depth, " +
        "weather, recentweather, trendtype, cloudtype, windshear, cavok. Prefix a field with trend, tempo or becmg (e.g. tempoceiling) to read the trend";

    /// <summary>
    /// Looks up a field by name or alias, ignoring case.
    /// </summary>
    /// <param name="name">The name as typed.</param>
    /// <param name="field">The field, when found.</param>
    /// <returns>True if the name is a known field.</returns>
    public static bool TryGetField(string name, out WeatherField field)
    {
        return TryGetField(name, out field, out _);
    }

    /// <summary>
    /// Looks up a field by name or alias, ignoring case. A trend field is a field the trend can carry with "trend",
    /// "tempo" or "becmg" in front, e.g. trendceiling or tempoweather.
    /// </summary>
    /// <param name="name">The name as typed.</param>
    /// <param name="field">The field, when found.</param>
    /// <param name="trend">Which trend groups the field is read from, or none for the METAR.</param>
    /// <returns>True if the name is a known field.</returns>
    public static bool TryGetField(string name, out WeatherField field, out TrendScope trend)
    {
        trend = TrendScope.None;
        if (s_fields.TryGetValue(name, out field))
        {
            return true;
        }

        foreach (var (prefix, scope) in s_trendPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                s_fields.TryGetValue(name[prefix.Length..], out field) && s_trendFields.Contains(field))
            {
                trend = scope;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the name used for a unit when writing a condition, e.g. <c>ft</c> or <c>kt</c>.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The unit as typed in a condition.</returns>
    public static string GetUnitName(WeatherUnit unit)
    {
        return s_units.First(x => x.Value == unit).Key;
    }

    // A number is a whole or decimal number, or a fraction such as 1/2 as in 1/2SM.
    [GeneratedRegex(@"^([+-]?\d+(?:\.\d+)?(?:/\d+)?)(.*)$")]
    internal static partial Regex NumberPattern();

    // The fraction of a mixed number, e.g. the 1/2SM in "1 1/2SM".
    [GeneratedRegex(@"^(\d+)/(\d+)(.*)$")]
    internal static partial Regex FractionPattern();
}
