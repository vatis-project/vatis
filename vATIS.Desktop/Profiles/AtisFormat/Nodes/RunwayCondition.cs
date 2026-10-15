// <copyright file="RunwayCondition.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Linq;

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
    /// Gets or sets the wording of the deposit, depth and friction terms, keyed by term name.
    /// The depth terms use <c>{value}</c> as a placeholder for the depth.
    /// </summary>
    public Dictionary<string, RunwayConditionTerm> Terms { get; set; } = DefaultTerms();

    /// <summary>
    /// Gets the default wording of all runway condition terms.
    /// </summary>
    /// <returns>A new dictionary containing the default terms.</returns>
    public static Dictionary<string, RunwayConditionTerm> DefaultTerms() => new()
    {
        ["ClearAndDry"] = new("CLEAR AND DRY", "clear and dry"),
        ["Damp"] = new("DAMP", "damp"),
        ["Wet"] = new("WET", "wet"),
        ["Rime"] = new("COVERED WITH RIME", "covered with rime"),
        ["DrySnow"] = new("COVERED WITH DRY SNOW", "covered with dry snow"),
        ["WetSnow"] = new("COVERED WITH WET SNOW", "covered with wet snow"),
        ["Slush"] = new("COVERED WITH SLUSH", "covered with slush"),
        ["Ice"] = new("COVERED WITH ICE", "covered with ice"),
        ["CompactedSnow"] = new("COVERED WITH COMPACTED SNOW", "covered with compacted snow"),
        ["FrozenRuts"] = new("FROZEN RUTS", "frozen ruts"),
        ["NotOperational"] = new("NOT OPERATIONAL DUE TO DEPOSITS", "not operational due to deposits"),
        ["DepthMillimeter"] = new("{value}MM", "{value} millimeter"),
        ["DepthMillimeters"] = new("{value}MM", "{value} millimeters"),
        ["DepthCentimeters"] = new("{value}CM", "{value} centimeters"),
        ["FrictionGood"] = new("FRICTION GOOD", "estimated surface friction good"),
        ["FrictionMediumToGood"] = new("FRICTION MEDIUM TO GOOD", "estimated surface friction medium to good"),
        ["FrictionMedium"] = new("FRICTION MEDIUM", "estimated surface friction medium"),
        ["FrictionMediumToPoor"] = new("FRICTION MEDIUM TO POOR", "estimated surface friction medium to poor"),
        ["FrictionPoor"] = new("FRICTION POOR", "estimated surface friction poor"),
        ["FrictionUnreliable"] = new("FRICTION UNRELIABLE", "estimated surface friction unreliable"),
    };

    /// <summary>
    /// Creates a new instance of <see cref="RunwayCondition"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="RunwayCondition"/> instance that is a copy of this instance.</returns>
    public RunwayCondition Clone()
    {
        var clone = (RunwayCondition)MemberwiseClone();
        clone.Terms = Terms.ToDictionary(x => x.Key, x => new RunwayConditionTerm(x.Value.Text, x.Value.Voice));
        return clone;
    }
}
