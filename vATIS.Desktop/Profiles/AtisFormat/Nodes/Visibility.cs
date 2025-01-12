// <copyright file="Visibility.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

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
    /// Gets or sets the voice description for unlimited visibility.
    /// </summary>
    public string UnlimitedVisibilityVoice { get; set; } = "visibility 10 kilometers or more";

    /// <summary>
    /// Gets or sets the text description for unlimited visibility.
    /// </summary>
    public string UnlimitedVisibilityText { get; set; } = "VIS 10KM";

    /// <summary>
    /// Gets or sets a value indicating whether to include the visibility suffix.
    /// </summary>
    public bool IncludeVisibilitySuffix { get; set; } = true;

    /// <summary>
    /// Gets or sets the visibility meters cutoff.
    /// </summary>
    public int MetersCutoff { get; set; } = 5000;

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
