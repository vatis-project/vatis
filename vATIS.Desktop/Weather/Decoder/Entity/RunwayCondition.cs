// <copyright file="RunwayCondition.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Weather.Decoder.Entity;

/// <summary>
/// Represents a runway state group in a METAR, e.g. <c>R25/290150</c>.
/// </summary>
public class RunwayCondition
{
    /// <summary>
    /// Describes the type of deposit on the runway surface.
    /// </summary>
    public enum DepositType
    {
        /// <summary>Clear and dry.</summary>
        ClearAndDry = 0,

        /// <summary>Damp.</summary>
        Damp = 1,

        /// <summary>Wet or water patches.</summary>
        Wet = 2,

        /// <summary>Covered with rime or frost.</summary>
        Rime = 3,

        /// <summary>Covered with dry snow.</summary>
        DrySnow = 4,

        /// <summary>Covered with wet snow.</summary>
        WetSnow = 5,

        /// <summary>Covered with slush.</summary>
        Slush = 6,

        /// <summary>Covered with ice.</summary>
        Ice = 7,

        /// <summary>Covered with compacted or rolled snow.</summary>
        CompactedSnow = 8,

        /// <summary>Frozen ruts or ridges.</summary>
        FrozenRuts = 9,
    }

    /// <summary>
    /// Describes the estimated surface friction.
    /// </summary>
    public enum FrictionType
    {
        /// <summary>Good (0.40 and above).</summary>
        Good,

        /// <summary>Medium to good (0.36 - 0.39).</summary>
        MediumToGood,

        /// <summary>Medium (0.30 - 0.35).</summary>
        Medium,

        /// <summary>Medium to poor (0.26 - 0.29).</summary>
        MediumToPoor,

        /// <summary>Poor (0.17 - 0.25).</summary>
        Poor,

        /// <summary>Unreliable (below 0.17, or not reliably measurable).</summary>
        Unreliable,
    }

    /// <summary>
    /// Gets or sets the runway identifier, e.g. 25L.
    /// </summary>
    public string Runway { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw runway state group as it appeared in the METAR, e.g. R25/290150.
    /// </summary>
    public string RawValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deposit type, if reported.
    /// </summary>
    public DepositType? Deposit { get; set; }

    /// <summary>
    /// Gets or sets the deposit depth in millimeters, if reported and significant.
    /// </summary>
    public int? DepthMillimeters { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the runway is reported as non-operational due to deposits.
    /// </summary>
    public bool NotOperational { get; set; }

    /// <summary>
    /// Gets or sets the estimated friction coefficient (0.00 - 0.90), if reported as such.
    /// </summary>
    public double? FrictionCoefficient { get; set; }

    /// <summary>
    /// Gets or sets the estimated surface friction quality, if reported.
    /// </summary>
    public FrictionType? Friction { get; set; }
}
