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

    private static readonly ConcurrentDictionary<string, WeatherExpression?> s_weatherConditions = new();
    private static readonly ConcurrentDictionary<string, StationExpression?> s_stationConditions = new();

    /// <summary>
    /// Returns the enabled definitions whose station and weather conditions are met, ordered by ordinal.
    /// </summary>
    /// <param name="definitions">The definitions to filter.</param>
    /// <param name="onlineCallsigns">The online controller callsigns, or null when unknown. Unknown is treated as
    /// nobody online.</param>
    /// <returns>The applicable definitions.</returns>
    /// <param name="metar">The current decoded METAR, or null when none is available.</param>
    /// <param name="atisFormat">The station format, used to find the ceiling layer. Weather rules are not met when
    /// this is null.</param>
    /// <param name="utcNow">The current time in UTC, or null to use the system clock.</param>
    /// <param name="forced">Definitions the user turned on by hand, which are included whether or not their
    /// conditions are met. They must still be enabled.</param>
    /// <param name="ordinalOverrides">Positions the user set by hand, used in place of a definition's ordinal.</param>
    /// <param name="hidden">Definitions the user turned off by hand, which are left out whatever their conditions.</param>
    public static List<StaticDefinition> GetActive(
        IEnumerable<StaticDefinition> definitions, IReadOnlyCollection<string>? onlineCallsigns,
        DecodedMetar? metar = null, AtisFormat? atisFormat = null, DateTime? utcNow = null,
        IReadOnlySet<StaticDefinition>? forced = null,
        IReadOnlyDictionary<StaticDefinition, int>? ordinalOverrides = null,
        IReadOnlySet<StaticDefinition>? hidden = null)
    {
        return definitions
            .Where(x => x.Enabled && hidden?.Contains(x) != true &&
                        (forced?.Contains(x) == true || IsConditionMet(x, onlineCallsigns, metar, atisFormat, utcNow)))
            .OrderBy(x => ordinalOverrides != null && ordinalOverrides.TryGetValue(x, out var ordinal)
                ? ordinal
                : x.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Determines whether any definition depends on controllers being online.
    /// </summary>
    /// <param name="definitions">The definitions to check.</param>
    /// <returns>True if at least one enabled definition has a station condition.</returns>
    public static bool HasConditions(IEnumerable<StaticDefinition> definitions)
    {
        return definitions.Any(x => x.Enabled && IsConditional(x));
    }

    /// <summary>
    /// Determines whether any enabled definition depends on the time of day, so it needs checking again as time
    /// passes.
    /// </summary>
    /// <param name="definitions">The definitions to check.</param>
    /// <returns>True if at least one enabled definition has a time range in its controller condition.</returns>
    public static bool HasTimeConditions(IEnumerable<StaticDefinition> definitions)
    {
        return definitions.Any(x => x.Enabled && IsConditional(x) &&
                                    GetStationExpression(x.OnlineCondition!)?.HasTimeCondition == true);
    }

    /// <summary>
    /// Determines whether any enabled definition depends on a METAR value.
    /// </summary>
    /// <param name="definitions">The definitions to check.</param>
    /// <returns>True if at least one enabled definition has a weather condition.</returns>
    public static bool HasWeatherConditions(IEnumerable<StaticDefinition> definitions)
    {
        return definitions.Any(x => x.Enabled && !string.IsNullOrWhiteSpace(x.WeatherCondition));
    }

    /// <summary>
    /// Determines whether the definition's station and weather conditions are met.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <param name="onlineCallsigns">The online controller callsigns, or null when unknown.</param>
    /// <param name="metar">The current decoded METAR, or null when none is available.</param>
    /// <param name="atisFormat">The station format, used to find the ceiling layer.</param>
    /// <param name="utcNow">The current time in UTC, or null to use the system clock.</param>
    /// <returns>True if the definition applies.</returns>
    public static bool IsConditionMet(
        StaticDefinition definition, IReadOnlyCollection<string>? onlineCallsigns,
        DecodedMetar? metar = null, AtisFormat? atisFormat = null, DateTime? utcNow = null)
    {
        if (!string.IsNullOrWhiteSpace(definition.WeatherCondition) &&
            (atisFormat == null || !IsWeatherConditionMet(definition.WeatherCondition, metar, atisFormat)))
        {
            return false;
        }

        if (!IsConditional(definition))
        {
            return true;
        }

        // A condition that does not parse is never met.
        return GetStationExpression(definition.OnlineCondition!)?.Evaluate(onlineCallsigns, utcNow) ?? false;
    }

    /// <summary>
    /// Determines whether the definition has a controller or weather condition.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <returns>True if the definition can be left out because of a condition.</returns>
    public static bool HasCondition(StaticDefinition definition)
    {
        return IsConditional(definition) || !string.IsNullOrWhiteSpace(definition.WeatherCondition);
    }

    private static StationExpression? GetStationExpression(string condition)
    {
        if (!s_stationConditions.TryGetValue(condition, out var expression))
        {
            // The editor refuses to save a condition that does not parse, so this only happens for a hand-edited
            // profile.
            StationExpression.TryParse(condition, out expression, out _);
            if (s_stationConditions.Count >= MaxCachedConditions)
            {
                s_stationConditions.Clear();
            }

            s_stationConditions[condition] = expression;
        }

        return expression;
    }

    private static bool IsWeatherConditionMet(string condition, DecodedMetar? metar, AtisFormat atisFormat)
    {
        if (!s_weatherConditions.TryGetValue(condition, out var expression))
        {
            // A condition that does not parse is never met. The editor refuses to save one, so this only happens for
            // a hand-edited profile.
            WeatherExpression.TryParse(condition, out expression, out _);
            if (s_weatherConditions.Count >= MaxCachedConditions)
            {
                s_weatherConditions.Clear();
            }

            s_weatherConditions[condition] = expression;
        }

        return expression?.Evaluate(metar, atisFormat) ?? false;
    }

    private static bool IsConditional(StaticDefinition definition)
    {
        return !string.IsNullOrWhiteSpace(definition.OnlineCondition);
    }
}
