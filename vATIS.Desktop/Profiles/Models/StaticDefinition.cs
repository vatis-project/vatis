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
    /// Gets or sets a condition on which VATSIM controllers are online that must be met for this definition to be
    /// included, written as text, e.g. <c>LAS_E_GND NOT LAS_W_GND</c>. A <c>*</c> in a callsign matches any run of
    /// characters. See <see cref="StationExpression"/> for the syntax.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OnlineCondition { get; set; }

    /// <summary>
    /// Gets or sets the station condition from before conditions were written as text. Only read from old profiles
    /// and converted to <see cref="OnlineCondition"/> when loaded.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public StationCondition StationCondition { get; set; }

    /// <summary>
    /// Gets or sets the callsigns for the old <see cref="StationCondition"/>. Only read from old profiles and
    /// converted to <see cref="OnlineCondition"/> when loaded.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StationCallsigns { get; set; }

    /// <summary>
    /// Gets or sets a condition on the METAR that must be met for this definition to be included, written as text,
    /// e.g. <c>ceiling &lt; 200ft AND (rvr &lt; 550m OR visibility &lt; 100m)</c>. Combined with
    /// <see cref="OnlineCondition"/>: both must pass.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WeatherCondition { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the static definition is enabled.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => this.RaiseAndSetIfChanged(ref _enabled, value);
    }

    /// <summary>
    /// Converts a station condition from an old profile to an <see cref="OnlineCondition"/>.
    /// </summary>
    public void OnDeserialized()
    {
        if (string.IsNullOrWhiteSpace(OnlineCondition))
        {
            OnlineCondition = StationExpression.FromLegacy(StationCondition, StationCallsigns);
        }

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
            OnlineCondition = OnlineCondition,
            WeatherCondition = WeatherCondition,
        };
    }
}
