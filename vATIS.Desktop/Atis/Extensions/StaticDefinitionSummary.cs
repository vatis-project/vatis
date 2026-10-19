// <copyright file="StaticDefinitionSummary.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Describes in words when a static definition is included, for the message lists.
/// </summary>
public static partial class StaticDefinitionSummary
{
    /// <summary>
    /// Gets one line for each condition on the definition, or nothing if it is always included.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The lines, e.g. <c>When online: EPWA_DEL</c> and <c>Weather: ceiling &lt; 200ft</c>.</returns>
    public static IReadOnlyList<string> Describe(StaticDefinition definition)
    {
        var lines = new List<string>();

        if (definition.StationCondition != StationCondition.Always &&
            !string.IsNullOrWhiteSpace(definition.StationCallsigns))
        {
            var callsigns = string.Join(
                ", ", definition.StationCallsigns.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries));
            lines.Add($"{(definition.StationCondition == StationCondition.WhenOnline ? "When online" : "When offline")}: {callsigns}");
        }

        if (!string.IsNullOrWhiteSpace(definition.WeatherCondition))
        {
            lines.Add($"Weather: {Whitespace().Replace(definition.WeatherCondition.Trim(), " ")}");
        }

        return lines;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
