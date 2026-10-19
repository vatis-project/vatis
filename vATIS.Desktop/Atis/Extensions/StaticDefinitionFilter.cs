// <copyright file="StaticDefinitionFilter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public static List<StaticDefinition> GetActive(
        IEnumerable<StaticDefinition> definitions, IReadOnlyCollection<string>? onlineCallsigns,
        DecodedMetar? metar = null, AtisFormat? atisFormat = null)
    {
        return definitions
            .Where(x => x.Enabled && IsConditionMet(x, onlineCallsigns, metar, atisFormat))
            .OrderBy(x => x.Ordinal)
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
    /// <returns>True if the definition applies.</returns>
    public static bool IsConditionMet(
        StaticDefinition definition, IReadOnlyCollection<string>? onlineCallsigns,
        DecodedMetar? metar = null, AtisFormat? atisFormat = null)
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

        var patterns = ParsePatterns(definition.StationCallsigns);
        var anyOnline = onlineCallsigns != null &&
                        onlineCallsigns.Any(callsign => patterns.Any(p => p.IsMatch(callsign)));

        return definition.StationCondition == StationCondition.WhenOnline ? anyOnline : !anyOnline;
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
        return definition.StationCondition != StationCondition.Always &&
               !string.IsNullOrWhiteSpace(definition.StationCallsigns);
    }

    private static List<Regex> ParsePatterns(string? callsigns)
    {
        return (callsigns ?? string.Empty)
            .Split([',', ' ', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => new Regex("^" + Regex.Escape(x.Trim()).Replace("\\*", ".*") + "$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .ToList();
    }
}
