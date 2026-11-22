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
    /// Gets one line for the condition on the definition, or nothing if it is always included.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>The lines, e.g. <c>Controllers and weather: EPWA_DEL AND ceiling &lt; 200ft</c>.</returns>
    public static IReadOnlyList<string> Describe(StaticDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Condition))
        {
            return [];
        }

        return [$"{Label(definition.Condition)}: {Whitespace().Replace(definition.Condition.Trim(), " ")}"];
    }

    // The condition can test controllers, the time, the weather and the runways, in any mix.
    private static string Label(string condition)
    {
        if (!ConditionExpression.TryParse(condition, out var expression, out _) || expression == null)
        {
            return "Condition";
        }

        var parts = new List<string>();
        if (expression.HasCallsignCondition)
        {
            parts.Add("controllers");
        }

        if (expression.HasTimeCondition)
        {
            parts.Add("time");
        }

        if (expression.HasWeatherCondition)
        {
            parts.Add("weather");
        }

        if (expression.HasRunwayCondition)
        {
            parts.Add("runways");
        }

        if (parts.Count == 0)
        {
            return "Condition";
        }

        var text = parts.Count == 1 ? parts[0] : string.Join(", ", parts[..^1]) + " and " + parts[^1];
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
