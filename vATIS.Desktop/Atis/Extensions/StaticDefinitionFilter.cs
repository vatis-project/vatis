// <copyright file="StaticDefinitionFilter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Selects the static definitions that apply, given which VATSIM controllers are online.
/// </summary>
public static class StaticDefinitionFilter
{
    private const int MaxCachedConditions = 256;

    private static readonly ConcurrentDictionary<string, ConditionExpression?> s_conditions = new();

    /// <summary>
    /// Returns the enabled definitions whose condition is met, ordered by ordinal.
    /// </summary>
    /// <param name="definitions">The definitions to filter.</param>
    /// <param name="onlineCallsigns">The online controller callsigns, or null when unknown. Unknown is treated as
    /// nobody online.</param>
    /// <param name="metar">The current decoded METAR, or null when none is available.</param>
    /// <param name="atisFormat">The station format, used to find the ceiling layer. Weather terms are not met when
    /// this is null.</param>
    /// <param name="utcNow">The current time in UTC, or null to use the system clock.</param>
    /// <param name="forced">Definitions the user turned on by hand, which are included whether or not their
    /// conditions are met. They must still be enabled.</param>
    /// <param name="ordinalOverrides">Positions the user set by hand, used in place of a definition's ordinal.</param>
    /// <param name="hidden">Definitions the user turned off by hand, which are left out whatever their conditions.</param>
    /// <param name="runways">The runways in use, or null when none are known.</param>
    /// <returns>The applicable definitions.</returns>
    public static List<StaticDefinition> GetActive(
        IEnumerable<StaticDefinition> definitions, IReadOnlyCollection<string>? onlineCallsigns,
        DecodedMetar? metar = null, AtisFormat? atisFormat = null, DateTime? utcNow = null,
        IReadOnlySet<StaticDefinition>? forced = null,
        IReadOnlyDictionary<StaticDefinition, int>? ordinalOverrides = null,
        IReadOnlySet<StaticDefinition>? hidden = null, ActiveRunways? runways = null)
    {
        return definitions
            .Where(x => x.Enabled && hidden?.Contains(x) != true &&
                        (forced?.Contains(x) == true ||
                         IsConditionMet(x, onlineCallsigns, metar, atisFormat, utcNow, runways)))
            .OrderBy(x => ordinalOverrides != null && ordinalOverrides.TryGetValue(x, out var ordinal)
                ? ordinal
                : x.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Determines whether any definition has a condition, so what it says can change as things around it change.
    /// </summary>
    /// <param name="definitions">The definitions to check.</param>
    /// <returns>True if at least one enabled definition has a condition.</returns>
    public static bool HasConditions(IEnumerable<StaticDefinition> definitions)
    {
        return definitions.Any(x => x.Enabled && HasCondition(x));
    }

    /// <summary>
    /// Determines whether any enabled definition depends on the time of day, so it needs checking again as time
    /// passes.
    /// </summary>
    /// <param name="definitions">The definitions to check.</param>
    /// <returns>True if at least one enabled definition has a time range in its condition.</returns>
    public static bool HasTimeConditions(IEnumerable<StaticDefinition> definitions)
    {
        return definitions.Any(x => x.Enabled && GetExpression(x)?.HasTimeCondition == true);
    }

    /// <summary>
    /// Determines whether any enabled definition depends on a METAR value.
    /// </summary>
    /// <param name="definitions">The definitions to check.</param>
    /// <returns>True if at least one enabled definition has a weather term in its condition.</returns>
    public static bool HasWeatherConditions(IEnumerable<StaticDefinition> definitions)
    {
        return definitions.Any(x => x.Enabled && GetExpression(x)?.HasWeatherCondition == true);
    }

    /// <summary>
    /// Determines whether any enabled definition depends on the runways in use.
    /// </summary>
    /// <param name="definitions">The definitions to check.</param>
    /// <returns>True if at least one enabled definition has a runway term in its condition.</returns>
    public static bool HasRunwayConditions(IEnumerable<StaticDefinition> definitions)
    {
        return definitions.Any(x => x.Enabled && GetExpression(x)?.HasRunwayCondition == true);
    }

    /// <summary>
    /// Determines whether the definition's condition is met.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <param name="onlineCallsigns">The online controller callsigns, or null when unknown.</param>
    /// <param name="metar">The current decoded METAR, or null when none is available.</param>
    /// <param name="atisFormat">The station format, used to find the ceiling layer.</param>
    /// <param name="utcNow">The current time in UTC, or null to use the system clock.</param>
    /// <param name="runways">The runways in use, or null when none are known.</param>
    /// <returns>True if the definition applies.</returns>
    public static bool IsConditionMet(
        StaticDefinition definition, IReadOnlyCollection<string>? onlineCallsigns,
        DecodedMetar? metar = null, AtisFormat? atisFormat = null, DateTime? utcNow = null,
        ActiveRunways? runways = null)
    {
        if (!HasCondition(definition))
        {
            return true;
        }

        // A condition that does not parse is never met. The editor refuses to save one, so this only happens for a
        // hand-edited profile.
        return GetExpression(definition)
            ?.Evaluate(new ConditionContext(onlineCallsigns, metar, atisFormat, utcNow, runways)) ?? false;
    }

    /// <summary>
    /// Determines whether the definition has a condition.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>True if the definition can be left out because of a condition.</returns>
    public static bool HasCondition(StaticDefinition definition)
    {
        return !string.IsNullOrWhiteSpace(definition.Condition);
    }

    private static ConditionExpression? GetExpression(StaticDefinition definition)
    {
        var condition = definition.Condition;
        if (string.IsNullOrWhiteSpace(condition))
        {
            return null;
        }

        if (!s_conditions.TryGetValue(condition, out var expression))
        {
            ConditionExpression.TryParse(condition, out expression, out _);
            if (s_conditions.Count >= MaxCachedConditions)
            {
                s_conditions.Clear();
            }

            s_conditions[condition] = expression;
        }

        return expression;
    }
}
