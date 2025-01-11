// <copyright file="SurfaceWindNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides the surface wind information.
/// </summary>
public class SurfaceWindNode : BaseNode<SurfaceWind>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.SurfaceWind);
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(SurfaceWind node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        var magVarDeg = Station.AtisFormat.SurfaceWind.MagneticVariation?.MagneticDegrees ?? null;
        var leadingZero = Station.AtisFormat.SurfaceWind.SpeakLeadingZero ? "00" : "";

        var meanDirection = node.MeanDirection?.ActualValue ?? null;
        var meanSpeed = node.MeanSpeed?.ActualValue ?? null;
        var speedVariations = node.SpeedVariations?.ActualValue ?? null;
        var directionVariations = node.DirectionVariations ?? null;

        int? minDirectionVariation = null;
        if (directionVariations != null)
        {
            minDirectionVariation = (int)directionVariations[0].ActualValue;
        }

        int? maxDirectionVariation = null;
        if (directionVariations != null)
        {
            maxDirectionVariation = (int)directionVariations[1].ActualValue;
        }

        int? meanSpeedKts = null;
        if (node.MeanSpeed != null)
        {
            meanSpeedKts = SurfaceWind.ToKts(node.MeanSpeed).ToInt32(CultureInfo.InvariantCulture);
        }

        int? meanSpeedMps = null;
        if (node.MeanSpeed != null)
        {
            meanSpeedMps = SurfaceWind.ToMps(node.MeanSpeed).ToInt32(CultureInfo.InvariantCulture);
        }

        int? speedVariationKts = null;
        if (node.SpeedVariations != null)
        {
            speedVariationKts = SurfaceWind.ToKts(node.SpeedVariations).ToInt32(CultureInfo.InvariantCulture);
        }

        int? speedVariationMps = null;
        if (node.SpeedVariations != null)
        {
            speedVariationMps = SurfaceWind.ToMps(node.SpeedVariations).ToInt32(CultureInfo.InvariantCulture);
        }

        var magVarEnabled = Station.AtisFormat.SurfaceWind.MagneticVariation?.Enabled ?? false;
        var meanDirectionMag = (meanDirection ?? 0).ApplyMagVar(magVarEnabled, magVarDeg).ToString("000");

        format = Regex.Replace(format, "{wind_dir}", meanDirectionMag.ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_spd}", meanSpeed?.ToString(leadingZero).ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{wind_spd\|kt}", meanSpeedKts?.ToString(leadingZero).ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{wind_spd\|mps}", meanSpeedMps?.ToString(leadingZero).ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_gust}", speedVariations?.ToString(leadingZero).ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{wind_gust\|kt}", speedVariationKts?.ToString(leadingZero).ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{wind_gust\|mps}", speedVariationMps?.ToString(leadingZero).ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_vmin}", minDirectionVariation?.ApplyMagVar(magVarEnabled, magVarDeg).ToString("000").ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_vmax}", maxDirectionVariation?.ApplyMagVar(magVarEnabled, magVarDeg).ToString("000").ToSerialFormat() ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_unit}", GetSpokenWindUnit(node.MeanSpeed), RegexOptions.IgnoreCase);

        return format;
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(SurfaceWind node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        var magVarDeg = Station.AtisFormat.SurfaceWind.MagneticVariation?.MagneticDegrees ?? null;

        var meanDirection = node.MeanDirection?.ActualValue ?? null;
        var meanSpeed = node.MeanSpeed?.ActualValue ?? null;
        var speedVariations = node.SpeedVariations?.ActualValue ?? null;
        var directionVariations = node.DirectionVariations ?? null;

        int? minDirectionVariation = null;
        if (directionVariations != null)
        {
            minDirectionVariation = (int)directionVariations[0].ActualValue;
        }

        int? maxDirectionVariation = null;
        if (directionVariations != null)
        {
            maxDirectionVariation = (int)directionVariations[1].ActualValue;
        }

        int? meanSpeedKts = null;
        if (node.MeanSpeed != null)
        {
            meanSpeedKts = SurfaceWind.ToKts(node.MeanSpeed).ToInt32(CultureInfo.InvariantCulture);
        }

        int? meanSpeedMps = null;
        if (node.MeanSpeed != null)
        {
            meanSpeedMps = SurfaceWind.ToMps(node.MeanSpeed).ToInt32(CultureInfo.InvariantCulture);
        }

        int? speedVariationKts = null;
        if (node.SpeedVariations != null)
        {
            speedVariationKts = SurfaceWind.ToKts(node.SpeedVariations).ToInt32(CultureInfo.InvariantCulture);
        }

        int? speedVariationMps = null;
        if (node.SpeedVariations != null)
        {
            speedVariationMps = SurfaceWind.ToMps(node.SpeedVariations).ToInt32(CultureInfo.InvariantCulture);
        }

        var magVarEnabled = Station.AtisFormat.SurfaceWind.MagneticVariation?.Enabled ?? false;
        var meanDirectionMag = (meanDirection ?? 0).ApplyMagVar(magVarEnabled, magVarDeg).ToString("000");

        format = Regex.Replace(format, "{wind_dir}", meanDirectionMag, RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_spd}", meanSpeed?.ToString("00") ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{wind_spd\|kt}", meanSpeedKts?.ToString("00") ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{wind_spd\|mps}", meanSpeedMps?.ToString("00") ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_gust}", speedVariations?.ToString("00") ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{wind_gust\|kt}", speedVariationKts?.ToString("00") ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, @"{wind_gust\|mps}", speedVariationMps?.ToString("00") ?? "", RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_vmin}", minDirectionVariation?.ApplyMagVar(magVarEnabled, magVarDeg).ToString("000") ?? "",
            RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_vmax}", maxDirectionVariation?.ApplyMagVar(magVarEnabled, magVarDeg).ToString("000") ?? "",
            RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind_unit}", node.SpeedUnit.ToString(), RegexOptions.IgnoreCase);
        format = Regex.Replace(format, "{wind}", node.RawValue ?? "", RegexOptions.IgnoreCase);

        return format;
    }

    private void Parse(SurfaceWind? surfaceWind)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (surfaceWind == null)
            return;

        List<string> spokenAtis = [];
        List<string> textAtis = [];

        if (surfaceWind.SpeedVariations != null)
        {
            // VRB10G20KT
            if (surfaceWind.VariableDirection)
            {
                var voice = ParseVoiceVariables(surfaceWind, Station.AtisFormat.SurfaceWind.VariableGust.Template.Voice);
                spokenAtis.Add(voice);

                var text = ParseTextVariables(surfaceWind, Station.AtisFormat.SurfaceWind.VariableGust.Template.Text);
                textAtis.Add(text);
            }

            // 25010G16KT
            else
            {
                var voice = ParseVoiceVariables(surfaceWind, Station.AtisFormat.SurfaceWind.StandardGust.Template.Voice);
                spokenAtis.Add(voice);

                var text = ParseTextVariables(surfaceWind, Station.AtisFormat.SurfaceWind.StandardGust.Template.Text);
                textAtis.Add(text);
            }
        }

        // 25010KT
        else
        {
            if (surfaceWind.MeanDirection != null)
            {
                // calm wind
                if ((surfaceWind.MeanDirection.ActualValue == 0 && surfaceWind.MeanSpeed?.ActualValue == 0) ||
                    surfaceWind.MeanSpeed?.ActualValue <= Station.AtisFormat.SurfaceWind.Calm.CalmWindSpeed)
                {
                    var voice = ParseVoiceVariables(surfaceWind, Station.AtisFormat.SurfaceWind.Calm.Template.Voice);
                    spokenAtis.Add(voice);

                    var text = ParseTextVariables(surfaceWind, Station.AtisFormat.SurfaceWind.Calm.Template.Text);
                    textAtis.Add(text);
                }
                else
                {
                    var voice = ParseVoiceVariables(surfaceWind, Station.AtisFormat.SurfaceWind.Standard.Template.Voice);
                    spokenAtis.Add(voice);

                    var text = ParseTextVariables(surfaceWind, Station.AtisFormat.SurfaceWind.Standard.Template.Text);
                    textAtis.Add(text);
                }
            }
        }

        // VRB10KT
        if (surfaceWind.SpeedVariations == null && surfaceWind is { MeanSpeed: not null, VariableDirection: true })
        {
            var voice = ParseVoiceVariables(surfaceWind, Station.AtisFormat.SurfaceWind.Variable.Template.Voice);
            spokenAtis.Add(voice);

            var text = ParseTextVariables(surfaceWind, Station.AtisFormat.SurfaceWind.Variable.Template.Text);
            textAtis.Add(text);
        }

        // 250V360
        if (surfaceWind.DirectionVariations != null)
        {
            var voice = ParseVoiceVariables(surfaceWind, Station.AtisFormat.SurfaceWind.VariableDirection.Template.Voice);
            spokenAtis.Add(voice);

            var text = ParseTextVariables(surfaceWind, Station.AtisFormat.SurfaceWind.VariableDirection.Template.Text);
            textAtis.Add(text);
        }

        VoiceAtis = string.Join(", ", spokenAtis).TrimEnd(',').TrimEnd(' ');
        TextAtis = string.Join(" ", textAtis).TrimEnd(' ');
    }

    private string GetSpokenWindUnit(Value? unit)
    {
        if (unit == null)
            return "";

        return unit.ActualUnit switch
        {
            Value.Unit.KilometerPerHour => unit.ActualValue > 1 ? "kilometers per hour" : "kilometer per hour",
            Value.Unit.MeterPerSecond => unit.ActualValue > 1 ? "meters per second" : "meter per second",
            Value.Unit.Knot => unit.ActualValue > 1 ? "knots" : "knot",
            _ => ""
        };
    }
}
