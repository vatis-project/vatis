// <copyright file="Visibility.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the visibility component of the ATIS format.
/// </summary>
public class Visibility : BaseFormat, IJsonOnDeserialized
{
    private List<VisibilityFormatRule>? _formatRules;

    /// <summary>
    /// Initializes a new instance of the <see cref="Visibility"/> class.
    /// </summary>
    public Visibility()
    {
        Template = new Template { Text = "{visibility}", Voice = "VISIBILITY {visibility}", };
    }

    /// <summary>
    /// Gets or sets the visibility description to the north.
    /// </summary>
    public string North { get; set; } = "to the north";

    /// <summary>
    /// Gets or sets the visibility description to the north-east.
    /// </summary>
    public string NorthEast { get; set; } = "to the north-east";

    /// <summary>
    /// Gets or sets the visibility description to the east.
    /// </summary>
    public string East { get; set; } = "to the east";

    /// <summary>
    /// Gets or sets the visibility description to the south-east.
    /// </summary>
    public string SouthEast { get; set; } = "to the south-east";

    /// <summary>
    /// Gets or sets the visibility description to the south.
    /// </summary>
    public string South { get; set; } = "to the south";

    /// <summary>
    /// Gets or sets the visibility description to the south-west.
    /// </summary>
    public string SouthWest { get; set; } = "to the south-west";

    /// <summary>
    /// Gets or sets the visibility description to the west.
    /// </summary>
    public string West { get; set; } = "to the west";

    /// <summary>
    /// Gets or sets the visibility description to the north-west.
    /// </summary>
    public string NorthWest { get; set; } = "to the north-west";

    /// <summary>
    /// Gets or sets the text ATIS output for the directional minimum visibility, keyed by compass direction.
    /// </summary>
    public Dictionary<string, string> DirectionText { get; set; } = new()
    {
        { "N", "N" },
        { "NE", "NE" },
        { "E", "E" },
        { "SE", "SE" },
        { "S", "S" },
        { "SW", "SW" },
        { "W", "W" },
        { "NW", "NW" },
    };

    /// <summary>
    /// Gets or sets the rules describing how a metric visibility is written in the text ATIS and spoken in the voice
    /// ATIS.
    /// </summary>
    public List<VisibilityFormatRule> FormatRules
    {
        get => _formatRules ??= VisibilityFormatRule.Defaults();
        set => _formatRules = value;
    }

    /// <summary>
    /// Gets or sets the former voice description for unlimited visibility. Only read to migrate older profiles.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UnlimitedVisibilityVoice { get; set; }

    /// <summary>
    /// Gets or sets the former text description for unlimited visibility. Only read to migrate older profiles.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UnlimitedVisibilityText { get; set; }

    /// <summary>
    /// Gets or sets the voice description for CAVOK.
    /// </summary>
    public string CavokVoice { get; set; } = "CAVOK";

    /// <summary>
    /// Gets or sets the text description for CAVOK.
    /// </summary>
    public string CavokText { get; set; } = "CAVOK";

    /// <summary>
    /// Gets or sets the former option to include the visibility suffix. Only read to migrate older profiles.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IncludeVisibilitySuffix { get; set; }

    /// <summary>
    /// Gets or sets the former visibility meters cutoff. Only read to migrate older profiles.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MetersCutoff { get; set; }

    /// <summary>
    /// Gets or sets the former spoken unit word for meters. Only read to migrate older profiles.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MetersUnitVoice { get; set; }

    /// <summary>
    /// Gets or sets the former spoken unit word for kilometers. Only read to migrate older profiles.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? KilometersUnitVoice { get; set; }

    /// <summary>
    /// Gets or sets the spoken form of fractional statute mile visibility, keyed by the raw METAR value.
    /// </summary>
    public Dictionary<string, string> FractionVoice { get; set; } = new()
    {
        { "M1/4SM", "less than one quarter." },
        { "1 1/8SM", "one and one eighth." },
        { "1 1/4SM", "one and one quarter." },
        { "1 3/8SM", "one and three eighths." },
        { "1 1/2SM", "one and one half." },
        { "1 5/8SM", "one and five eighths." },
        { "1 3/4SM", "one and three quarters." },
        { "1 7/8SM", "one and seven eighths." },
        { "2 1/4SM", "two and one quarter." },
        { "2 1/2SM", "two and one half." },
        { "2 3/4SM", "two and three quarters." },
        { "1/16SM", "one sixteenth." },
        { "1/8SM", "one eighth." },
        { "3/16SM", "three sixteenths." },
        { "1/4SM", "one quarter." },
        { "5/16SM", "five sixteenths." },
        { "3/8SM", "three eighths." },
        { "1/2SM", "one half." },
        { "5/8SM", "five eighths." },
        { "3/4SM", "three quarters." },
        { "7/8SM", "seven eighths." },
    };

    /// <summary>
    /// Sets the unlimited visibility voice description.
    /// </summary>
    [JsonPropertyName("UnlimitedVisibility")]
    private string UnlimitedVisibility
    {
        set => UnlimitedVisibilityVoice = value;
    }

    /// <summary>
    /// Migrates the former cutoff, suffix and unit word settings of older profiles to format rules.
    /// </summary>
    void IJsonOnDeserialized.OnDeserialized()
    {
        if (_formatRules == null &&
            (MetersCutoff != null || IncludeVisibilitySuffix != null ||
             MetersUnitVoice != null || KilometersUnitVoice != null ||
             UnlimitedVisibilityText != null || UnlimitedVisibilityVoice != null))
        {
            _formatRules = VisibilityFormatRule.FromLegacy(
                MetersCutoff ?? 5000,
                IncludeVisibilitySuffix ?? true,
                MetersUnitVoice ?? "meters",
                KilometersUnitVoice ?? "kilometers",
                StripTemplate(Template.Text, UnlimitedVisibilityText ?? "VIS 10KM"),
                StripTemplate(Template.Voice, UnlimitedVisibilityVoice ?? "visibility 10 kilometers or more"));
        }

        IncludeVisibilitySuffix = null;
        MetersCutoff = null;
        MetersUnitVoice = null;
        KilometersUnitVoice = null;
        UnlimitedVisibilityText = null;
        UnlimitedVisibilityVoice = null;
    }

    /// <summary>
    /// Removes the text around {visibility} in the template from a former 9999 value, which replaced the whole
    /// template, so that it can be used as the value of a rule instead.
    /// </summary>
    private static string StripTemplate(string? template, string value)
    {
        var index = template?.IndexOf("{visibility}", StringComparison.OrdinalIgnoreCase) ?? -1;
        if (template == null || index < 0)
        {
            return value;
        }

        var prefix = template[..index].Trim();
        var suffix = template[(index + "{visibility}".Length)..].Trim();
        var result = value.Trim();

        if (prefix.Length > 0 && result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            result = result[prefix.Length..].TrimStart();
        }

        if (suffix.Length > 0 && result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            result = result[..^suffix.Length].TrimEnd();
        }

        return result;
    }

    /// <summary>
    /// Gets the spoken description of a compass direction.
    /// </summary>
    /// <param name="direction">The compass direction (e.g. N, NE).</param>
    /// <returns>The spoken description, or an empty string for an unknown direction.</returns>
    public string GetDirectionVoice(string direction) => direction switch
    {
        "N" => North,
        "NE" => NorthEast,
        "E" => East,
        "SE" => SouthEast,
        "S" => South,
        "SW" => SouthWest,
        "W" => West,
        "NW" => NorthWest,
        _ => string.Empty,
    };

    /// <summary>
    /// Sets the spoken description of a compass direction.
    /// </summary>
    /// <param name="direction">The compass direction (e.g. N, NE).</param>
    /// <param name="voice">The spoken description.</param>
    public void SetDirectionVoice(string direction, string voice)
    {
        switch (direction)
        {
            case "N": North = voice; break;
            case "NE": NorthEast = voice; break;
            case "E": East = voice; break;
            case "SE": SouthEast = voice; break;
            case "S": South = voice; break;
            case "SW": SouthWest = voice; break;
            case "W": West = voice; break;
            case "NW": NorthWest = voice; break;
        }
    }

    /// <summary>
    /// Creates a new instance of <see cref="Visibility"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Visibility"/> instance that is a copy of this instance.</returns>
    public Visibility Clone()
    {
        var clone = (Visibility)MemberwiseClone();
        clone.FormatRules = [.. FormatRules];
        return clone;
    }
}
