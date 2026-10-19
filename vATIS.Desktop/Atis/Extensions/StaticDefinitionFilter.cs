// <copyright file="StaticDefinitionFilter.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Selects the static definitions that apply, given which VATSIM controllers are online.
/// </summary>
public static class StaticDefinitionFilter
{
    /// <summary>
    /// Returns the enabled definitions whose station condition is met, ordered by ordinal.
    /// </summary>
    /// <param name="definitions">The definitions to filter.</param>
    /// <param name="onlineCallsigns">The online controller callsigns, or null when unknown. Unknown is treated as
    /// nobody online.</param>
    /// <returns>The applicable definitions.</returns>
    public static List<StaticDefinition> GetActive(
        IEnumerable<StaticDefinition> definitions, IReadOnlyCollection<string>? onlineCallsigns)
    {
        return definitions
            .Where(x => x.Enabled && IsConditionMet(x, onlineCallsigns))
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
    /// Determines whether the definition's station condition is met.
    /// </summary>
    /// <param name="definition">The definition.</param>
    /// <param name="onlineCallsigns">The online controller callsigns, or null when unknown.</param>
    /// <returns>True if the definition applies.</returns>
    public static bool IsConditionMet(StaticDefinition definition, IReadOnlyCollection<string>? onlineCallsigns)
    {
        if (!IsConditional(definition))
        {
            return true;
        }

        var patterns = ParsePatterns(definition.StationCallsigns);
        var anyOnline = onlineCallsigns != null &&
                        onlineCallsigns.Any(callsign => patterns.Any(p => p.IsMatch(callsign)));

        return definition.StationCondition == StationCondition.WhenOnline ? anyOnline : !anyOnline;
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
