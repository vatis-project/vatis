// <copyright file="WindShear.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the WS component of the ATIS format.
/// </summary>
public class WindShear : BaseFormat
{
    /// <summary>
    /// Gets or sets the text for individual runway wind shear.
    /// </summary>
    public string? RunwayText { get; set; } = "WS R{Runway}";

    /// <summary>
    /// Gets or sets the voice for individual runway wind shear.
    /// </summary>
    public string? RunwayVoice { get; set; } = "Wind Shear Runway {Runway}";

    /// <summary>
    /// Gets or sets the text for all runway wind shears.
    /// </summary>
    public string? AllRunwayText { get; set; } = "WS ALL RWY";

    /// <summary>
    /// Gets or sets the voice for all runway wind shears.
    /// </summary>
    public string? AllRunwayVoice { get; set; } = "Wind Shear All Runways";

    /// <summary>
    /// Creates a new instance of <see cref="WindShear"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="WindShear"/> instance that is a copy of this instance.</returns>
    public WindShear Clone()
    {
        return (WindShear)MemberwiseClone();
    }
}
