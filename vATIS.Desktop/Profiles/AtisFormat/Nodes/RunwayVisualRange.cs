// <copyright file="RunwayVisualRange.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the runway visual range component of the ATIS format.
/// </summary>
public class RunwayVisualRange : BaseFormat
{
    /// <summary>
    /// Gets or sets the neutral tendency.
    /// </summary>
    public string? NeutralTendency { get; set; } = "Neutral";

    /// <summary>
    /// Gets or sets the going up tendency.
    /// </summary>
    public string? GoingUpTendency { get; set; } = "Going Up";

    /// <summary>
    /// Gets or sets the going down tendency.
    /// </summary>
    public string? GoingDownTendency { get; set; } = "Going Down";

    /// <summary>
    /// Creates a new instance of <see cref="RunwayVisualRange"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="RunwayVisualRange"/> instance that is a copy of this instance.</returns>
    public RunwayVisualRange Clone()
    {
        return (RunwayVisualRange)MemberwiseClone();
    }
}
