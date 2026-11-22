// <copyright file="ConditionContext.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Vatsim.Vatis.Profiles.AtisFormat;
using Vatsim.Vatis.Profiles.Models;
using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Atis.Extensions;

/// <summary>
/// Everything a <see cref="ConditionExpression"/> can look at.
/// </summary>
/// <param name="OnlineCallsigns">The online controller callsigns, or null when unknown. Unknown is treated as nobody
/// online.</param>
/// <param name="Metar">The decoded METAR, or null when none is available.</param>
/// <param name="AtisFormat">The station format, which decides which cloud layers count as a ceiling. Weather terms
/// cannot be answered without it.</param>
/// <param name="UtcNow">The current time in UTC, or null to use the system clock.</param>
/// <param name="Runways">The runways in use, or null when none are known.</param>
public sealed record ConditionContext(
    IReadOnlyCollection<string>? OnlineCallsigns = null,
    DecodedMetar? Metar = null,
    AtisFormat? AtisFormat = null,
    DateTime? UtcNow = null,
    ActiveRunways? Runways = null);

/// <summary>
/// The runways in use for a preset.
/// </summary>
/// <param name="Arrival">The arrival runways, e.g. <c>07L</c>.</param>
/// <param name="Departure">The departure runways.</param>
public sealed record ActiveRunways(IReadOnlyCollection<string> Arrival, IReadOnlyCollection<string> Departure)
{
    /// <summary>
    /// Gets an instance with no runways.
    /// </summary>
    public static ActiveRunways None { get; } = new([], []);

    /// <summary>
    /// Reads the runways from a preset.
    /// </summary>
    /// <param name="preset">The preset, or null.</param>
    /// <returns>The preset's runways.</returns>
    public static ActiveRunways From(AtisPreset? preset)
    {
        return preset == null ? None : new ActiveRunways(Split(preset.ArrivalRunways), Split(preset.DepartureRunways));
    }

    /// <summary>
    /// Gathers the runways of all the presets, for suggesting names while a condition is typed.
    /// </summary>
    /// <param name="presets">The presets.</param>
    /// <returns>Every arrival runway and every departure runway the presets list.</returns>
    public static ActiveRunways FromPresets(IEnumerable<AtisPreset> presets)
    {
        var all = presets.Select(From).ToList();
        return new ActiveRunways(
            all.SelectMany(x => x.Arrival).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            all.SelectMany(x => x.Departure).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>
    /// Splits a list of runways separated by commas, semicolons or spaces.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The runways.</returns>
    public static string[] Split(string? text)
    {
        return (text ?? string.Empty).Split([',', ';', ' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }
}
