// <copyright file="VisibilityFormatRule.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ReactiveUI;
using Vatsim.Vatis.Atis.Extensions;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a rule describing how a metric visibility is written in the text ATIS and spoken in the voice ATIS. The
/// rule applies to visibilities from <see cref="FromMeters"/> up to the next higher rule.
/// </summary>
public record VisibilityFormatRule(int FromMeters, string Text, string Voice) : ReactiveRecord
{
    /// <summary>
    /// The text token for the visibility as reported in the METAR (four digits, e.g. 0800).
    /// </summary>
    public const string RawToken = "{raw}";

    /// <summary>
    /// The token for the visibility in meters (e.g. 800 in the text ATIS, "eight hundred" in the voice ATIS).
    /// </summary>
    public const string MetersToken = "{m}";

    /// <summary>
    /// The token for the visibility in kilometers (e.g. 7 or 6.5).
    /// </summary>
    public const string KilometersToken = "{km}";

    /// <summary>
    /// Gets the default rules, which write the METAR value in the text ATIS, speak meters below 5000 and kilometers
    /// from 5000, and report 9999 as 10 kilometers or more.
    /// </summary>
    /// <returns>The default rule list.</returns>
    public static List<VisibilityFormatRule> Defaults() =>
    [
        new VisibilityFormatRule(0, RawToken, MetersToken + " meters"),
        new VisibilityFormatRule(5000, RawToken, KilometersToken + " kilometers"),
        new VisibilityFormatRule(9999, "10KM", "10 kilometers or more"),
    ];

    /// <summary>
    /// Creates rules equivalent to the former cutoff, suffix and unit word settings.
    /// </summary>
    /// <param name="metersCutoff">The visibility above which kilometers were spoken.</param>
    /// <param name="includeSuffix">Whether the unit word was spoken.</param>
    /// <param name="metersUnit">The spoken word for meters.</param>
    /// <param name="kilometersUnit">The spoken word for kilometers.</param>
    /// <param name="unlimitedText">The text shown for a visibility of 9999.</param>
    /// <param name="unlimitedVoice">The text spoken for a visibility of 9999.</param>
    /// <returns>The equivalent rule list.</returns>
    public static List<VisibilityFormatRule> FromLegacy(
        int metersCutoff,
        bool includeSuffix,
        string metersUnit,
        string kilometersUnit,
        string unlimitedText,
        string unlimitedVoice) =>
    [
        new VisibilityFormatRule(0, RawToken, includeSuffix ? $"{MetersToken} {metersUnit}".TrimEnd() : MetersToken),
        new VisibilityFormatRule(
            metersCutoff + 1,
            RawToken,
            includeSuffix ? $"{KilometersToken} {kilometersUnit}".TrimEnd() : KilometersToken),
        new VisibilityFormatRule(9999, unlimitedText, unlimitedVoice),
    ];

    /// <summary>
    /// Determines whether the text ATIS shows a visibility in meters as reported in the METAR.
    /// </summary>
    /// <param name="rules">The rules to evaluate.</param>
    /// <param name="meters">The visibility in meters.</param>
    /// <returns><c>true</c> if the matching rule (or no rule) shows the METAR value.</returns>
    public static bool IsRawText(IEnumerable<VisibilityFormatRule>? rules, double meters)
    {
        var text = Match(rules, meters)?.Text;
        return text == null || string.Equals(text.Trim(), RawToken, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Formats a visibility in meters for the text ATIS.
    /// </summary>
    /// <param name="rules">The rules to evaluate.</param>
    /// <param name="meters">The visibility in meters.</param>
    /// <returns>The formatted visibility, or the METAR value if no rule matches.</returns>
    public static string ApplyText(IEnumerable<VisibilityFormatRule>? rules, double meters)
    {
        var whole = (int)meters;
        return Expand(
            Match(rules, meters)?.Text ?? RawToken,
            whole.ToString("D4", CultureInfo.InvariantCulture),
            whole.ToString(CultureInfo.InvariantCulture),
            meters);
    }

    /// <summary>
    /// Formats a visibility in meters for the voice ATIS.
    /// </summary>
    /// <param name="rules">The rules to evaluate.</param>
    /// <param name="meters">The visibility in meters.</param>
    /// <param name="groupForm">Whether to speak meters in group form (e.g. "twelve hundred").</param>
    /// <returns>The spoken visibility, or the meters in words if no rule matches.</returns>
    public static string ApplyVoice(IEnumerable<VisibilityFormatRule>? rules, double meters, bool groupForm = false)
    {
        var whole = (int)meters;
        return Expand(
            Match(rules, meters)?.Voice ?? MetersToken,
            string.Empty,
            groupForm ? whole.ToGroupForm() : whole.ToWordString(),
            meters).Trim();
    }

    /// <inheritdoc />
    public virtual bool Equals(VisibilityFormatRule? other)
    {
        return other != null && FromMeters == other.FromMeters && Text == other.Text && Voice == other.Voice;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(FromMeters, Text, Voice);
    }

    private static VisibilityFormatRule? Match(IEnumerable<VisibilityFormatRule>? rules, double meters)
    {
        return rules?
            .Where(r => r.FromMeters <= meters)
            .OrderByDescending(r => r.FromMeters)
            .FirstOrDefault();
    }

    private static string Expand(string format, string raw, string meters, double actual)
    {
        var km = (actual / 1000).ToString("0.###", CultureInfo.InvariantCulture);
        return format
            .Replace(RawToken, raw, StringComparison.OrdinalIgnoreCase)
            .Replace(MetersToken, meters, StringComparison.OrdinalIgnoreCase)
            .Replace(KilometersToken, km, StringComparison.OrdinalIgnoreCase);
    }
}
