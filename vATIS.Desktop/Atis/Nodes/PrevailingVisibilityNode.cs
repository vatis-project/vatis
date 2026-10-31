// <copyright file="PrevailingVisibilityNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.Models;
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
            !UsesRawText(value))
        {
            return Regex.Replace(format, "{visibility}", _ => FormatWithRules(value), RegexOptions.IgnoreCase);
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
                var rules = Station.AtisFormat.Visibility.FormatRules;
                var prevailingText = VisibilityFormatRule.ApplyVoice(rules, node.PrevailingVisibility.ActualValue);

                if (!string.IsNullOrEmpty(node.MinimumVisibilityDirection) && node.MinimumVisibility != null)
                {
                    var direction = Station.AtisFormat.Visibility.GetDirectionVoice(node.MinimumVisibilityDirection);

                    var minimumText = VisibilityFormatRule.ApplyVoice(rules, node.MinimumVisibility.ActualValue, true);
                    parsedValue.Add($"{prevailingText} and {direction} {minimumText}".Replace("  ", " "));
                }
                else
                {
                    parsedValue.Add(prevailingText);
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

    private bool UsesRawText(Visibility value)
    {
        var rules = Station!.AtisFormat.Visibility.FormatRules;
        return VisibilityFormatRule.IsRawText(rules, value.PrevailingVisibility!.ActualValue) &&
               (value.MinimumVisibility is not { ActualUnit: Value.Unit.Meter } ||
                VisibilityFormatRule.IsRawText(rules, value.MinimumVisibility.ActualValue));
    }

    private string FormatWithRules(Visibility value)
    {
        var rules = Station!.AtisFormat.Visibility.FormatRules;
        var text = VisibilityFormatRule.ApplyText(rules, value.PrevailingVisibility!.ActualValue);

        if (value.MinimumVisibility is { ActualUnit: Value.Unit.Meter })
        {
            var minimum = VisibilityFormatRule.ApplyText(rules, value.MinimumVisibility.ActualValue);
            text += " " + minimum;

            if (!string.IsNullOrEmpty(value.MinimumVisibilityDirection))
            {
                var direction = Station.AtisFormat.Visibility.DirectionText
                    .GetValueOrDefault(value.MinimumVisibilityDirection, value.MinimumVisibilityDirection);

                // Keep the METAR style (4000E) when the value ends in a digit, otherwise separate (4 KM E).
                text += minimum.Length > 0 && char.IsDigit(minimum[^1]) ? direction : " " + direction;
            }
        }

        return text;
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
