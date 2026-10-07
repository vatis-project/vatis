// <copyright file="AutoObservation.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the automatic observation component of the ATIS format.
/// </summary>
public class AutoObservation : BaseFormat
{
    /// <summary>
    /// Gets or sets the text template.
    /// </summary>
    public string? TextTemplate { get; set; } = "AUTO";

    /// <summary>
    /// Gets or sets the voice template.
    /// </summary>
    public string? VoiceTemplate { get; set; } = "Automatic Observation";

    /// <summary>
    /// Creates a new instance of <see cref="AutoObservation"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="AutoObservation"/> instance that is a copy of this instance.</returns>
    public AutoObservation Clone()
    {
        return (AutoObservation)MemberwiseClone();
    }
}
