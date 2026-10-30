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

        if (rule.Field == WeatherField.TrendType)
        {
            return IsTrendTypeMet(rule, metar);
        }

        return rule.Trend != TrendScope.None ? IsTrendMet(rule, metar, atisFormat) : Evaluate(rule, metar, atisFormat);
    }

    private static bool Evaluate(WeatherRule rule, DecodedMetar metar, AtisFormat atisFormat)
    {
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

    // A trend is checked as if it were a METAR of its own. Of the trend groups in scope, either one can meet a rule, but
    // all must meet a negative one (excludes), so that a trend with the code in it never counts as without it.
    private static bool IsTrendMet(WeatherRule rule, DecodedMetar metar, AtisFormat atisFormat)
    {
        var trends = new[] { metar.TrendForecast, metar.TrendForecastFuture }
            .Where(x => x != null && InScope(x, rule.Trend))
            .Select(x => TrendForecastDecoder.Decode(x!))
            .ToList();

        if (rule.Operator == WeatherOperator.NotContains)
        {
            return trends.All(x => Evaluate(rule, x, atisFormat));
        }

        return trends.Any(x => Evaluate(rule, x, atisFormat));
    }

    private static bool InScope(TrendForecast trend, TrendScope scope)
    {
        return scope switch
        {
            TrendScope.Tempo => trend.ChangeIndicator == TrendForecastType.Temporary,
            TrendScope.Becoming => trend.ChangeIndicator == TrendForecastType.Becoming,
            _ => true,
        };
    }

    // The trend type is the change indicator of either trend group, e.g. TEMPO or BECMG. Like the other trend rules,
    // either group can meet INCLUDES but both must meet EXCLUDES.
    private static bool IsTrendTypeMet(WeatherRule rule, DecodedMetar metar)
    {
        var types = new[] { metar.TrendForecast, metar.TrendForecastFuture }
            .Where(x => x != null)
            .Select(x => x!.ChangeIndicator switch
            {
                TrendForecastType.Temporary => "TEMPO",
                TrendForecastType.Becoming => "BECMG",
                TrendForecastType.NoSignificantChanges => "NOSIG",
                _ => string.Empty,
            })
            .ToList();

        var codes = GetCodes(rule);
        return rule.Operator == WeatherOperator.NotContains
            ? !types.Any(codes.Contains)
            : types.Any(codes.Contains);
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
            case WeatherField.RecentWeather:
                // Recent weather is reported with an RE prefix, e.g. RERA, so accept the code with or without it.
                var recent = (metar.RecentWeather?.RawValue ?? string.Empty).ToUpperInvariant();
                return recent.Length > 0 && GetCodes(rule)
                    .Select(code => code.StartsWith("RE", StringComparison.Ordinal) ? code[2..] : code)
                    .Any(code => code.Length > 0 && recent.Contains(code, StringComparison.Ordinal));
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
                // No ceiling layer means the ceiling is unlimited, so ceiling > 1000ft is met under clear skies.
                return GetCeilingLayer(metar, atisFormat) is { } ceiling
                    ? FromFeet(ceiling, rule.Unit)
                    : HasNoCeiling(metar) ? double.PositiveInfinity : null;
            case WeatherField.CloudBase:
                // No cloud layer means there is no cloud base, so cloudbase > 1000ft is met under clear skies.
                return FromFeet(
                    metar.Clouds.Where(x => x.BaseHeight is { ActualValue: > 0 })
                        .OrderBy(x => x.BaseHeight!.ActualValue).FirstOrDefault(),
                    rule.Unit) ?? (HasNoCeiling(metar) ? double.PositiveInfinity : null);
            case WeatherField.Rvr:
                // A variable RVR, e.g. 1800V3000FT, counts as its lowest value. No RVR reported means there is no
                // range limitation, so rvr > 125m is met, like an unlimited ceiling.
                var ranges = metar.RunwaysVisualRange
                    .Select(x => x.VisualRange != null ? x.VisualRange
                        : x.VisualRangeInterval?.OrderBy(ToMeters).FirstOrDefault())
                    .Where(x => x != null)
                    .Select(x => FromMeters(ToMeters(x!), rule.Unit))
                    .ToList();
                return ranges.Count == 0 ? double.PositiveInfinity : ranges.Min();
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
                // Wind without a gust group has no gust, which counts as a gust of zero so that gust < 25kt is met.
                return metar.SurfaceWind is { SpeedVariations: null } ? 0 : FromSpeed(metar.SurfaceWind?.SpeedVariations, rule.Unit);
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
                // No deposit reported means a depth of zero, so depth < 3mm is met.
                var depths = metar.RunwayConditions.Where(x => x.DepthMillimeters != null)
                    .Select(x => (double)x.DepthMillimeters!.Value).ToList();
                return depths.Count == 0 ? 0 : depths.Max();
            default:
                return null;
        }
    }

    // True when the METAR reports sky that has no ceiling, such as CAVOK, CLR, SKC, NSC, or only few and scattered
    // clouds. A METAR without any cloud information says nothing about the ceiling.
    private static bool HasNoCeiling(DecodedMetar metar)
    {
        return metar.Cavok || metar.Visibility is { IsCavok: true } || metar.Clouds.Any(x => x.Amount is
            CloudLayer.CloudAmount.Few or CloudLayer.CloudAmount.Scattered or CloudLayer.CloudAmount.Clear or
            CloudLayer.CloudAmount.SkyClear or CloudLayer.CloudAmount.NoSignificantClouds or
            CloudLayer.CloudAmount.NoCloudsDetected);
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
