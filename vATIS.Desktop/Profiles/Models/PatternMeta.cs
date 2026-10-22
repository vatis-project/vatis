// <copyright file="PatternMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a regular expression that is matched against the ATIS text, with separate replacements for the
/// written ATIS and the voice. The replacements can use capture groups (<c>$1</c>, <c>${name}</c>) and the
/// functions <c>$group</c>, <c>$digits</c>, <c>$spell</c> and <c>$phonetic</c>.
/// </summary>
public class PatternMeta
{
    /// <summary>
    /// Gets or sets the regular expression matched against the ATIS text.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the replacement used in the written ATIS.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the replacement used for the voice.
    /// </summary>
    public string? Voice { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="PatternMeta"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="PatternMeta"/> instance that is a copy of this instance.</returns>
    public PatternMeta Clone()
    {
        return new PatternMeta { Pattern = Pattern, Text = Text, Voice = Voice, };
    }
}
