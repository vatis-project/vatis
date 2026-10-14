// <copyright file="Visibility.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the visibility component of the ATIS format.
/// </summary>
public class Visibility : BaseFormat
{
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
    /// Gets or sets the voice description for unlimited visibility.
    /// </summary>
    public string UnlimitedVisibilityVoice { get; set; } = "visibility 10 kilometers or more";

    /// <summary>
    /// Gets or sets the text description for unlimited visibility.
    /// </summary>
    public string UnlimitedVisibilityText { get; set; } = "VIS 10KM";

    /// <summary>
    /// Gets or sets the voice description for CAVOK.
    /// </summary>
    public string CavokVoice { get; set; } = "CAVOK";

    /// <summary>
    /// Gets or sets the text description for CAVOK.
    /// </summary>
    public string CavokText { get; set; } = "CAVOK";

    /// <summary>
    /// Gets or sets a value indicating whether to include the visibility suffix.
    /// </summary>
    public bool IncludeVisibilitySuffix { get; set; } = true;

    /// <summary>
    /// Gets or sets the visibility meters cutoff.
    /// </summary>
    public int MetersCutoff { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the spoken unit word used for meters when the visibility suffix is included.
    /// </summary>
    public string MetersUnitVoice { get; set; } = "meters";

    /// <summary>
    /// Gets or sets the spoken unit word used for kilometers when the visibility suffix is included.
    /// </summary>
    public string KilometersUnitVoice { get; set; } = "kilometers";

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
    /// Creates a new instance of <see cref="Visibility"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="Visibility"/> instance that is a copy of this instance.</returns>
    public Visibility Clone()
    {
        return (Visibility)MemberwiseClone();
    }
}
