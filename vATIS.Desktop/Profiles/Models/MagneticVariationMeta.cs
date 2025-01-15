// <copyright file="MagneticVariationMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata related to magnetic variation, including its state and degree value.
/// </summary>
public class MagneticVariationMeta
{
    /// <summary>
    /// Gets or sets a value indicating whether the feature is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the magnetic variation degrees used for calculations or display.
    /// </summary>
    public int MagneticDegrees { get; set; }
}
