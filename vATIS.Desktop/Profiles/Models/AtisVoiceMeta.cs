// <copyright file="AtisVoiceMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata related to the ATIS voice configuration.
/// </summary>
public class AtisVoiceMeta
{
    /// <summary>
    /// Gets or sets a value indicating whether text-to-speech should be used for ATIS voice configuration.
    /// </summary>
    public bool UseTextToSpeech { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the voice to be used for ATIS voice configuration.
    /// </summary>
    public string? Voice { get; set; } = "Default";

    /// <summary>
    /// Creates a new instance of <see cref="AtisVoiceMeta"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="AtisVoiceMeta"/> instance that is a copy of this instance.</returns>
    public AtisVoiceMeta Clone()
    {
        return new AtisVoiceMeta
        {
            UseTextToSpeech = this.UseTextToSpeech,
            Voice = this.Voice,
        };
    }
}
