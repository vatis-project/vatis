// <copyright file="RunwayConditionNode.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Profiles.AtisFormat.Nodes;
using Vatsim.Vatis.Weather.Decoder.Entity;
using RunwayCondition = Vatsim.Vatis.Weather.Decoder.Entity.RunwayCondition;
using RunwayConditionFormat = Vatsim.Vatis.Profiles.AtisFormat.Nodes.RunwayCondition;

namespace Vatsim.Vatis.Atis.Nodes;

/// <summary>
/// Represents an ATIS node that provides runway condition information reported in the METAR.
/// </summary>
public class RunwayConditionNode : BaseNode<RunwayCondition>
{
    /// <inheritdoc/>
    public override void Parse(DecodedMetar metar)
    {
        var format = Station?.AtisFormat.RunwayCondition;
        if (format == null || metar.RunwayConditions.Count == 0)
        {
            return;
        }

        var text = new List<string>();
        var voice = new List<string>();

        foreach (var condition in metar.RunwayConditions)
        {
            text.Add(Apply(format, condition, voiceVariable: false));
            voice.Add(Apply(format, condition, voiceVariable: true));
        }

        TextAtis = string.Join(" ", text.Where(x => !string.IsNullOrWhiteSpace(x)));
        VoiceAtis = string.Join(", ", voice.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    /// <inheritdoc/>
    public override string ParseVoiceVariables(RunwayCondition node, string? format) =>
        throw new NotImplementedException();

    /// <inheritdoc/>
    public override string ParseTextVariables(RunwayCondition node, string? format) =>
        throw new NotImplementedException();

    private static string Apply(RunwayConditionFormat format, RunwayCondition condition, bool voiceVariable)
    {
        var template = voiceVariable ? format.VoiceTemplate : format.TextTemplate;
        var match = Regex.Match(condition.Runway, @"^(\d{2})([LCR]?)$");
        var number = match.Groups[1].Value;
        var designator = match.Groups[2].Value;

        var runway = voiceVariable
            ? $"{int.Parse(number, CultureInfo.InvariantCulture).ToSerialFormat(leadingZero: true)} {designator switch
            {
                "L" => "left",
                "R" => "right",
                "C" => "center",
                _ => string.Empty,
            }}".Trim()
            : condition.Runway;

        var result = (template ?? string.Empty)
            .Replace("{raw}", voiceVariable ? string.Empty : condition.RawValue, StringComparison.OrdinalIgnoreCase)
            .Replace("{runway}", runway, StringComparison.OrdinalIgnoreCase)
            .Replace("{deposit}", Deposit(format, condition, voiceVariable), StringComparison.OrdinalIgnoreCase)
            .Replace("{depth}", Depth(format, condition, voiceVariable), StringComparison.OrdinalIgnoreCase)
            .Replace("{friction}", Friction(format, condition, voiceVariable), StringComparison.OrdinalIgnoreCase);

        return Regex.Replace(result, @"\s+", " ").Trim();
    }

    private static string Term(RunwayConditionFormat format, string key, bool voice)
    {
        if (!format.Terms.TryGetValue(key, out var term) &&
            !RunwayConditionFormat.DefaultTerms().TryGetValue(key, out term))
        {
            return string.Empty;
        }

        return voice ? term.Voice : term.Text;
    }

    private static string Deposit(RunwayConditionFormat format, RunwayCondition condition, bool voice)
    {
        if (condition.NotOperational)
        {
            return Term(format, "NotOperational", voice);
        }

        return condition.Deposit is { } deposit ? Term(format, deposit.ToString(), voice) : string.Empty;
    }

    private static string Depth(RunwayConditionFormat format, RunwayCondition condition, bool voice)
    {
        if (condition.DepthMillimeters is not { } mm)
        {
            return string.Empty;
        }

        var (key, value) = mm >= 100
            ? ("DepthCentimeters", mm / 10)
            : (mm == 1 ? "DepthMillimeter" : "DepthMillimeters", mm);

        return Term(format, key, voice)
            .Replace("{value}", voice ? value.ToGroupForm() : value.ToString(CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
    }

    private static string Friction(RunwayConditionFormat format, RunwayCondition condition, bool voice)
    {
        return condition.Friction is { } friction ? Term(format, $"Friction{friction}", voice) : string.Empty;
    }
}
