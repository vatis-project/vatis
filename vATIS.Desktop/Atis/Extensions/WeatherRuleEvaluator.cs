// <copyright file="WeatherRuleEvaluator.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;
using Vatsim.Vatis.Weather.Extensions;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Evaluates a single <see cref="WeatherRule"/> against a decoded METAR.
/// </summary>
public static class WeatherRuleEvaluator
{
    private const double FeetToMeters = 0.3048;
    private const double MetersPerStatuteMile = 1609.34;
    private const double KnotsToMetersPerSecond = 0.514444;
    private const double KilometersPerHourToMetersPerSecond = 0.277778;
    private const double HectoPascalsPerInchOfMercury = 33.86389;
    private const double Tolerance = 0.001;

    // Visibility reported as CAVOK is at least this far.
    private const double CavokVisibilityMeters = 9999;

    private static readonly char[] s_codeSeparators = [',', ' ', ';'];

    /// <summary>
    /// Determines whether a rule is met. A rule is not met when the METAR does not report the value it needs.
    /// </summary>
    /// <param name="rule">The rule.</param>
    /// <param name="metar">The decoded METAR, or null when none is available.</param>
    /// <param name="atisFormat">The station format, which decides which cloud layers count as a ceiling.</param>
    /// <returns>True if the rule is met.</returns>
    public static bool IsMet(WeatherRule rule, DecodedMetar? metar, AtisFormat atisFormat)
    {
        if (metar == null)
        {
            return false;
        }

        if (WeatherFieldInfo.GetKind(rule.Field) != WeatherFieldKind.Numeric)
        {
            var present = IsPresent(rule, metar);
            return rule.Operator == WeatherOperator.NotContains ? !present : present;
        }

        if (GetNumber(rule, metar, atisFormat) is not { } actual)
        {
            return false;
        }

        return rule.Operator switch
        {
            WeatherOperator.Below => actual < rule.Value,
            WeatherOperator.Above => actual > rule.Value,
            WeatherOperator.AtOrBelow => actual <= rule.Value + Tolerance,
            WeatherOperator.AtOrAbove => actual >= rule.Value - Tolerance,
            WeatherOperator.EqualTo => Math.Abs(actual - rule.Value) < Tolerance,
            _ => false,
        };
    }

    private static bool IsPresent(WeatherRule rule, DecodedMetar metar)
    {
        switch (rule.Field)
        {
            case WeatherField.Windshear:
                return metar.WindshearAllRunways == true || metar.WindshearRunways is { Count: > 0 };
            case WeatherField.Cavok:
                return metar.Cavok || metar.Visibility is { IsCavok: true };
            case WeatherField.PresentWeather:
                var raw = metar.PresentWeather
                    .Select(x => (x.RawValue ?? string.Empty).ToUpperInvariant())
                    .ToList();
                return GetCodes(rule).Any(code => raw.Any(r => r.Contains(code, StringComparison.Ordinal)));
            case WeatherField.CloudType:
                var types = metar.Clouds.Select(x => x.Type switch
                {
                    CloudLayer.CloudType.Cumulonimbus => "CB",
                    CloudLayer.CloudType.ToweringCumulus => "TCU",
                    _ => string.Empty,
                }).Where(x => x.Length > 0).ToList();
                return GetCodes(rule).Any(code => types.Contains(code));
            default:
                return false;
        }
    }

    private static List<string> GetCodes(WeatherRule rule)
    {
        return (rule.Text ?? string.Empty)
            .ToUpperInvariant()
            .Split(s_codeSeparators, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static double? GetNumber(WeatherRule rule, DecodedMetar metar, AtisFormat atisFormat)
    {
        switch (rule.Field)
        {
            case WeatherField.Ceiling:
                return FromFeet(GetCeilingLayer(metar, atisFormat), rule.Unit);
            case WeatherField.CloudBase:
                return FromFeet(
                    metar.Clouds.Where(x => x.BaseHeight is { ActualValue: > 0 })
                        .OrderBy(x => x.BaseHeight!.ActualValue).FirstOrDefault(),
                    rule.Unit);
            case WeatherField.Rvr:
                // A variable RVR, e.g. 1800V3000FT, counts as its lowest value.
                var ranges = metar.RunwaysVisualRange
                    .Select(x => x.VisualRange != null ? x.VisualRange
                        : x.VisualRangeInterval?.OrderBy(ToMeters).FirstOrDefault())
                    .Where(x => x != null)
                    .Select(x => FromMeters(ToMeters(x!), rule.Unit))
                    .ToList();
                return ranges.Count == 0 ? null : ranges.Min();
            case WeatherField.Visibility:
                if (metar.Cavok || metar.Visibility is { IsCavok: true })
                {
                    return FromMeters(CavokVisibilityMeters, rule.Unit);
                }

                return metar.Visibility?.PrevailingVisibility is { } visibility
                    ? FromMeters(ToMeters(visibility), rule.Unit)
                    : null;
            case WeatherField.WindSpeed:
                return FromSpeed(metar.SurfaceWind?.MeanSpeed, rule.Unit);
            case WeatherField.WindGust:
                return FromSpeed(metar.SurfaceWind?.SpeedVariations, rule.Unit);
            case WeatherField.WindDirection:
                return metar.SurfaceWind is { VariableDirection: false, MeanDirection: { } direction }
                    ? direction.ActualValue
                    : null;
            case WeatherField.Temperature:
                return FromCelsius(metar.AirTemperature, rule.Unit);
            case WeatherField.Dewpoint:
                return FromCelsius(metar.DewPointTemperature, rule.Unit);
            case WeatherField.Pressure:
                return FromPressure(metar.Pressure?.Value, rule.Unit);
            case WeatherField.RunwayDepth:
                var depths = metar.RunwayConditions.Where(x => x.DepthMillimeters != null)
                    .Select(x => (double)x.DepthMillimeters!.Value).ToList();
                return depths.Count == 0 ? null : depths.Max();
            default:
                return null;
        }
    }

    // Vertical visibility (VV) means the sky is obscured, which is an indefinite ceiling at that height, so it counts
    // as a ceiling whether or not the station's format lists it as a ceiling layer type.
    private static CloudLayer? GetCeilingLayer(DecodedMetar metar, AtisFormat atisFormat)
    {
        var ceiling = atisFormat.GetCeilingLayer(metar.Clouds);
        var obscured = metar.Clouds
            .Where(x => x is { Amount: CloudLayer.CloudAmount.VerticalVisibility, BaseHeight.ActualValue: > 0 })
            .OrderBy(x => x.BaseHeight!.ActualValue)
            .FirstOrDefault();

        return ceiling == null || (obscured != null && obscured.BaseHeight!.ActualValue < ceiling.BaseHeight!.ActualValue)
            ? obscured ?? ceiling
            : ceiling;
    }

    // The decoder stores cloud base heights in hundreds of feet, whatever their unit says.
    private static double? FromFeet(CloudLayer? layer, WeatherUnit unit)
    {
        if (layer?.BaseHeight is not { } height)
        {
            return null;
        }

        var feet = height.ActualValue * 100;
        return unit == WeatherUnit.Meters ? feet * FeetToMeters : feet;
    }

    private static double ToMeters(Value value)
    {
        return value.ActualUnit switch
        {
            Value.Unit.Feet => value.ActualValue * FeetToMeters,
            Value.Unit.StatuteMile => value.ActualValue * MetersPerStatuteMile,
            _ => value.ActualValue,
        };
    }

    private static double FromMeters(double meters, WeatherUnit unit)
    {
        return unit switch
        {
            WeatherUnit.Feet => meters / FeetToMeters,
            WeatherUnit.Kilometers => meters / 1000,
            WeatherUnit.StatuteMiles => meters / MetersPerStatuteMile,
            _ => meters,
        };
    }

    private static double? FromSpeed(Value? speed, WeatherUnit unit)
    {
        if (speed == null)
        {
            return null;
        }

        var metersPerSecond = speed.ActualUnit switch
        {
            Value.Unit.Knot => speed.ActualValue * KnotsToMetersPerSecond,
            Value.Unit.KilometerPerHour => speed.ActualValue * KilometersPerHourToMetersPerSecond,
            _ => speed.ActualValue,
        };

        return unit switch
        {
            WeatherUnit.Knots => Math.Round(metersPerSecond / KnotsToMetersPerSecond, 3),
            WeatherUnit.KilometersPerHour => Math.Round(metersPerSecond / KilometersPerHourToMetersPerSecond, 3),
            _ => Math.Round(metersPerSecond, 3),
        };
    }

    private static double? FromCelsius(Value? temperature, WeatherUnit unit)
    {
        if (temperature == null)
        {
            return null;
        }

        return unit == WeatherUnit.Fahrenheit ? (temperature.ActualValue * 9 / 5) + 32 : temperature.ActualValue;
    }

    private static double? FromPressure(Value? pressure, WeatherUnit unit)
    {
        if (pressure == null)
        {
            return null;
        }

        // The decoder keeps the raw altimeter digits, so A2992 is stored as 2992 (hundredths of an inch).
        var hectoPascals = pressure.ActualUnit == Value.Unit.MercuryInch
            ? pressure.ActualValue / 100 * HectoPascalsPerInchOfMercury
            : pressure.ActualValue;

        return unit == WeatherUnit.InchesOfMercury
            ? Math.Round(hectoPascals / HectoPascalsPerInchOfMercury, 2)
            : Math.Round(hectoPascals, 2);
    }
}
