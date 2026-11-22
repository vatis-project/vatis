// <copyright file="StaticDefinition.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json.Serialization;
using ReactiveUI;
using Vatsim.Vatis.Atis.Extensions;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a static definition with text, ordinal, and enabled status.
/// </summary>
public class StaticDefinition : ReactiveObject, IJsonOnDeserialized
{
    private bool _enabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticDefinition"/> class.
    /// </summary>
    /// <param name="text">The text of the static definition.</param>
    /// <param name="ordinal">The ordinal position of the static definition.</param>
    /// <param name="enabled">A value indicating whether the static definition is enabled.</param>
    public StaticDefinition(string text, int ordinal, bool enabled = true)
    {
        Text = text;
        Ordinal = ordinal;
        Enabled = enabled;
    }

    /// <summary>
    /// Gets or sets the text of the static definition.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the ordinal position of the static definition.
    /// </summary>
    public int Ordinal { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a definition without text adds a single line break instead of a blank
    /// line between its neighbors.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool NewLineOnly { get; set; }

    /// <summary>
    /// Gets a value indicating whether this definition has no text and only adds spacing between its neighbors.
    /// </summary>
    [JsonIgnore]
    public bool IsSpacer => string.IsNullOrWhiteSpace(Text);

    /// <summary>
    /// Gets a value indicating whether this definition is a blank line, which adds an empty line between its
    /// neighbors.
    /// </summary>
    [JsonIgnore]
    public bool IsBlankLine => IsSpacer && !NewLineOnly;

    /// <summary>
    /// Gets a value indicating whether this definition is a new line, which adds a single line break between its
    /// neighbors.
    /// </summary>
    [JsonIgnore]
    public bool IsNewLine => IsSpacer && NewLineOnly;

    /// <summary>
    /// Gets or sets an optional separator placed after this definition, used exactly as typed. When null or empty,
    /// the station separator is used. Ignored for the last enabled definition.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SeparatorAfter { get; set; }

    /// <summary>
    /// Gets or sets a condition that must be met for this definition to be included, written as text. It can test
    /// which VATSIM controllers are online, the time of day, the METAR and the runways of the selected preset, and mix
    /// them with AND, OR and NOT, e.g. <c>EPKK_E_APP AND dep:07L AND NOT ceiling &lt; 200ft</c>. See
    /// <see cref="ConditionExpression"/> for the syntax.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Condition { get; set; }

    /// <summary>
    /// Gets or sets the controller condition from before conditions were combined. Only read from old profiles and
    /// merged into <see cref="Condition"/> when loaded.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OnlineCondition { get; set; }

    /// <summary>
    /// Gets or sets the weather condition from before conditions were combined. Only read from old profiles and
    /// merged into <see cref="Condition"/> when loaded.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WeatherCondition { get; set; }

    /// <summary>
    /// Gets or sets the station condition from before conditions were written as text. Only read from old profiles
    /// and merged into <see cref="Condition"/> when loaded.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public StationCondition StationCondition { get; set; }

    /// <summary>
    /// Gets or sets the callsigns for the old <see cref="StationCondition"/>. Only read from old profiles and merged
    /// into <see cref="Condition"/> when loaded.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StationCallsigns { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the static definition is enabled.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => this.RaiseAndSetIfChanged(ref _enabled, value);
    }

    /// <summary>
    /// Merges the separate station, controller and weather conditions of an old profile into <see cref="Condition"/>.
    /// </summary>
    public void OnDeserialized()
    {
        if (string.IsNullOrWhiteSpace(Condition))
        {
            var online = string.IsNullOrWhiteSpace(OnlineCondition)
                ? ConditionExpression.FromLegacy(StationCondition, StationCallsigns)
                : OnlineCondition;
            Condition = ConditionExpression.Combine(online, WeatherCondition);
        }

        OnlineCondition = null;
        WeatherCondition = null;
        StationCondition = StationCondition.Always;
        StationCallsigns = null;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return Text;
    }

    /// <summary>
    /// Creates a new instance of <see cref="StaticDefinition"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="StaticDefinition"/> instance that is a copy of this instance.</returns>
    public StaticDefinition Clone()
    {
        return new StaticDefinition(Text, Ordinal, Enabled)
        {
            SeparatorAfter = SeparatorAfter,
            Condition = Condition,
        };
    }
}
