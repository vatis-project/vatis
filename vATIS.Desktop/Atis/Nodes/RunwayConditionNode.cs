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
using Vatsim.Vatis.Weather.Decoder.Entity;

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
            text.Add(Apply(format.TextTemplate, condition, voiceVariable: false));
            voice.Add(Apply(format.VoiceTemplate, condition, voiceVariable: true));
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

    private static string Apply(string? template, RunwayCondition condition, bool voiceVariable)
    {
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
            .Replace("{runway}", runway, StringComparison.OrdinalIgnoreCase)
            .Replace("{deposit}", Deposit(condition, voiceVariable), StringComparison.OrdinalIgnoreCase)
            .Replace("{depth}", Depth(condition, voiceVariable), StringComparison.OrdinalIgnoreCase)
            .Replace("{friction}", Friction(condition, voiceVariable), StringComparison.OrdinalIgnoreCase);

        return Regex.Replace(result, @"\s+", " ").Trim();
    }

    private static string Deposit(RunwayCondition condition, bool voice)
    {
        if (condition.NotOperational)
        {
            return voice ? "not operational due to deposits" : "NOT OPERATIONAL DUE TO DEPOSITS";
        }

        var text = condition.Deposit switch
        {
            RunwayCondition.DepositType.ClearAndDry => "clear and dry",
            RunwayCondition.DepositType.Damp => "damp",
            RunwayCondition.DepositType.Wet => "wet",
            RunwayCondition.DepositType.Rime => "covered with rime",
            RunwayCondition.DepositType.DrySnow => "covered with dry snow",
            RunwayCondition.DepositType.WetSnow => "covered with wet snow",
            RunwayCondition.DepositType.Slush => "covered with slush",
            RunwayCondition.DepositType.Ice => "covered with ice",
            RunwayCondition.DepositType.CompactedSnow => "covered with compacted snow",
            RunwayCondition.DepositType.FrozenRuts => "frozen ruts",
            _ => string.Empty,
        };

        return voice ? text : text.ToUpperInvariant();
    }

    private static string Depth(RunwayCondition condition, bool voice)
    {
        if (condition.DepthMillimeters is not { } mm)
        {
            return string.Empty;
        }

        if (mm >= 100)
        {
            var cm = mm / 10;
            return voice ? $"{cm.ToGroupForm()} centimeters" : $"{cm}CM";
        }

        return voice ? $"{mm.ToGroupForm()} {(mm == 1 ? "millimeter" : "millimeters")}" : $"{mm}MM";
    }

    private static string Friction(RunwayCondition condition, bool voice)
    {
        var quality = condition.Friction switch
        {
            RunwayCondition.FrictionType.Good => "good",
            RunwayCondition.FrictionType.MediumToGood => "medium to good",
            RunwayCondition.FrictionType.Medium => "medium",
            RunwayCondition.FrictionType.MediumToPoor => "medium to poor",
            RunwayCondition.FrictionType.Poor => "poor",
            RunwayCondition.FrictionType.Unreliable => "unreliable",
            _ => null,
        };

        if (quality == null)
        {
            return string.Empty;
        }

        return voice ? $"estimated surface friction {quality}" : $"FRICTION {quality.ToUpperInvariant()}";
    }
}
