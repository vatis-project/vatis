// <copyright file="DensityAltitude.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the density altitude component of the ATIS format.
/// </summary>
public class DensityAltitude : BaseFormat
{
    /// <summary>
    /// Gets or sets the text template.
    /// </summary>
    public string? TextTemplate { get; set; } = "DENSITY ALTITUDE {altitude}FT";

    /// <summary>
    /// Gets or sets the voice template.
    /// </summary>
    public string? VoiceTemplate { get; set; } = "Density altitude {altitude} feet";

    /// <summary>
    /// Gets or sets how the <c>{altitude}</c> variable is written in the text and voice ATIS.
    /// </summary>
    public VariableFormat Format { get; set; } = new();

    /// <summary>
    /// Creates a new instance of <see cref="DensityAltitude"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="DensityAltitude"/> instance that is a copy of this instance.</returns>
    public DensityAltitude Clone()
    {
        return (DensityAltitude)MemberwiseClone();
    }
}
