// <copyright file="WeatherFieldInfo.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// What kind of value a <see cref="WeatherField"/> holds.
/// </summary>
public enum WeatherFieldKind
{
    /// <summary>
    /// A number compared with a threshold and unit.
    /// </summary>
    Numeric,

    /// <summary>
    /// Codes that are searched for in the METAR.
    /// </summary>
    Text,

    /// <summary>
    /// A condition that is either reported or not.
    /// </summary>
    Flag,
}

/// <summary>
/// Describes which operators and units are valid for each <see cref="WeatherField"/>, and how to label them.
/// </summary>
public static class WeatherFieldInfo
{
    private static readonly WeatherOperator[] s_numericOperators =
        [WeatherOperator.Below, WeatherOperator.Above, WeatherOperator.AtOrBelow, WeatherOperator.AtOrAbove,
            WeatherOperator.EqualTo];

    private static readonly WeatherOperator[] s_presenceOperators = [WeatherOperator.Contains, WeatherOperator.NotContains];

    private static readonly WeatherUnit[] s_heightUnits = [WeatherUnit.Feet, WeatherUnit.Meters];
    private static readonly WeatherUnit[] s_rvrUnits = [WeatherUnit.Meters, WeatherUnit.Kilometers, WeatherUnit.Feet];
    private static readonly WeatherUnit[] s_visibilityUnits =
        [WeatherUnit.Meters, WeatherUnit.Kilometers, WeatherUnit.Feet, WeatherUnit.StatuteMiles];

    private static readonly WeatherUnit[] s_speedUnits =
        [WeatherUnit.Knots, WeatherUnit.KilometersPerHour, WeatherUnit.MetersPerSecond];

    private static readonly WeatherUnit[] s_temperatureUnits = [WeatherUnit.Celsius, WeatherUnit.Fahrenheit];
    private static readonly WeatherUnit[] s_pressureUnits = [WeatherUnit.HectoPascals, WeatherUnit.InchesOfMercury];
    private static readonly WeatherUnit[] s_none = [];

    /// <summary>
    /// Gets the kind of value the field holds.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>The kind.</returns>
    public static WeatherFieldKind GetKind(WeatherField field)
    {
        return field switch
        {
            WeatherField.PresentWeather or WeatherField.CloudType => WeatherFieldKind.Text,
            WeatherField.Windshear or WeatherField.Cavok => WeatherFieldKind.Flag,
            _ => WeatherFieldKind.Numeric,
        };
    }

    /// <summary>
    /// Gets the operators that can be used with the field.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>The operators.</returns>
    public static IReadOnlyList<WeatherOperator> GetOperators(WeatherField field)
    {
        return GetKind(field) == WeatherFieldKind.Numeric ? s_numericOperators : s_presenceOperators;
    }

    /// <summary>
    /// Gets the units that can be used with the field. Empty when the field has no unit.
    /// </summary>
    /// <param name="field">The field.</param>
    /// <returns>The units, the first of which is the default.</returns>
    public static IReadOnlyList<WeatherUnit> GetUnits(WeatherField field)
    {
        return field switch
        {
            WeatherField.Ceiling or WeatherField.CloudBase => s_heightUnits,
            WeatherField.Rvr => s_rvrUnits,
            WeatherField.Visibility => s_visibilityUnits,
            WeatherField.WindSpeed or WeatherField.WindGust => s_speedUnits,
            WeatherField.WindDirection => [WeatherUnit.Degrees],
            WeatherField.Temperature or WeatherField.Dewpoint => s_temperatureUnits,
            WeatherField.Pressure => s_pressureUnits,
            WeatherField.RunwayDepth => [WeatherUnit.Millimeters],
            _ => s_none,
        };
    }

    /// <summary>
    /// Gets every field in display order.
    /// </summary>
    /// <returns>The fields.</returns>
    public static IReadOnlyList<WeatherField> GetFields()
    {
        return Enum.GetValues<WeatherField>();
    }
}
