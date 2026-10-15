// <copyright file="PrevailingVisibilityNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides the prevailing visibility.
/// </summary>
public class PrevailingVisibilityNode : BaseNode<Visibility>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        Parse(metar.Visibility);
    }

    /// <inheritdoc/>
    public override string ParseTextVariables(Visibility value, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        if (value.IsCavok)
        {
            return Station.AtisFormat.Visibility.CavokText;
        }

        if (value.PrevailingVisibility is { ActualUnit: Value.Unit.Meter } &&
            (int)value.PrevailingVisibility.ActualValue == 9999)
        {
            return Station.AtisFormat.Visibility.UnlimitedVisibilityText;
        }

        if (value.RawValue != null)
        {
            // A directional minimum (e.g. "7000 4000E") already carries its units; drop a unit suffix in the template.
            var pattern = value.MinimumVisibility != null ? "{visibility}M?" : "{visibility}";
            var raw = value.RawValue;
            if (!string.IsNullOrEmpty(value.MinimumVisibilityDirection))
            {
                var directionText = Station.AtisFormat.Visibility.DirectionText;
                raw = Regex.Replace(
                    raw,
                    @"(?<=\d{4})(NE|NW|SE|SW|N|E|S|W)\b",
                    m => directionText.GetValueOrDefault(m.Value, m.Value));
            }

            return Regex.Replace(format, pattern, _ => raw, RegexOptions.IgnoreCase);
        }

        return "";
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(Visibility node, string? format)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (format == null)
            return "";

        var parsedValue = new List<string>();

        if (node.IsCavok)
        {
            return Station.AtisFormat.Visibility.CavokVoice;
        }
        else
        {
            if (node.PrevailingVisibility is { ActualUnit: Value.Unit.Meter })
            {
                if ((int)node.PrevailingVisibility.ActualValue == 9999)
                {
                    return Station.AtisFormat.Visibility.UnlimitedVisibilityVoice;
                }

                if (!string.IsNullOrEmpty(node.MinimumVisibilityDirection) && node.MinimumVisibility != null)
                {
                    var suffix = Station.AtisFormat.Visibility.IncludeVisibilitySuffix;
                    var prevailing = node.PrevailingVisibility.ActualValue;
                    var prevailingText = prevailing > Station.AtisFormat.Visibility.MetersCutoff
                        ? $"{prevailing / 1000}{(suffix ? " " + Station.AtisFormat.Visibility.KilometersUnitVoice : "")}"
                        : $"{((int)prevailing).ToWordString()}{(suffix ? " " + Station.AtisFormat.Visibility.MetersUnitVoice : "")}";

                    var direction = node.MinimumVisibilityDirection switch
                    {
                        "N" => Station.AtisFormat.Visibility.North,
                        "NE" => Station.AtisFormat.Visibility.NorthEast,
                        "E" => Station.AtisFormat.Visibility.East,
                        "SE" => Station.AtisFormat.Visibility.SouthEast,
                        "S" => Station.AtisFormat.Visibility.South,
                        "SW" => Station.AtisFormat.Visibility.SouthWest,
                        "W" => Station.AtisFormat.Visibility.West,
                        "NW" => Station.AtisFormat.Visibility.NorthWest,
                        _ => string.Empty,
                    };

                    var minimum = (int)node.MinimumVisibility.ActualValue;
                    parsedValue.Add($"{prevailingText} and {direction} {minimum.ToGroupForm()}{(suffix ? " " + Station.AtisFormat.Visibility.MetersUnitVoice : "")}".Replace("  ", " "));
                }
                else
                {
                    if (node.PrevailingVisibility.ActualValue > Station.AtisFormat.Visibility.MetersCutoff)
                    {
                        parsedValue.Add($"{node.PrevailingVisibility.ActualValue / 1000} {(Station.AtisFormat.Visibility.IncludeVisibilitySuffix ? Station.AtisFormat.Visibility.KilometersUnitVoice : "")}");
                    }
                    else
                    {
                        var vis = (int)node.PrevailingVisibility.ActualValue;
                        parsedValue.Add($"{vis.ToWordString()} {(Station.AtisFormat.Visibility.IncludeVisibilitySuffix ? Station.AtisFormat.Visibility.MetersUnitVoice : "")}");
                    }
                }
            }
            else
            {
                if (node.RawValue != null && node.RawValue.Contains('/'))
                {
                    var result = Station.AtisFormat.Visibility.FractionVoice.GetValueOrDefault(node.RawValue, "");

                    parsedValue.Add(result);
                }
                else
                {
                    if (node.PrevailingVisibility != null)
                    {
                        var vis = (int)node.PrevailingVisibility.ActualValue;
                        parsedValue.Add(vis.ToString());
                    }
                }
            }

            return Regex.Replace(format, "{visibility}", string.Join(", ", parsedValue), RegexOptions.IgnoreCase);
        }
    }

    private void Parse(Visibility? node)
    {
        ArgumentNullException.ThrowIfNull(Station);

        if (node == null)
            return;

        VoiceAtis = ParseVoiceVariables(node, Station.AtisFormat.Visibility.Template.Voice);
        TextAtis = ParseTextVariables(node, Station.AtisFormat.Visibility.Template.Text);
    }
}
