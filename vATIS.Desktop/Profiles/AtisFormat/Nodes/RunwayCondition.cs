// <copyright file="RunwayCondition.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the runway condition component of the ATIS format.
/// </summary>
public class RunwayCondition : BaseFormat
{
    /// <summary>
    /// Gets or sets the text template, applied once per reported runway.
    /// </summary>
    public string? TextTemplate { get; set; } = "RWY {runway} {deposit} {depth} {friction}";

    /// <summary>
    /// Gets or sets the voice template, applied once per reported runway.
    /// </summary>
    public string? VoiceTemplate { get; set; } = "Runway {runway} {deposit} {depth} {friction}";

    /// <summary>
    /// Creates a new instance of <see cref="RunwayCondition"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="RunwayCondition"/> instance that is a copy of this instance.</returns>
    public RunwayCondition Clone()
    {
        return (RunwayCondition)MemberwiseClone();
    }
}
