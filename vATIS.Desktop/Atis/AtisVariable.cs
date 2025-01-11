// <copyright file="AtisVariable.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Atis;

/// <summary>
/// Represents an ATIS variable.
/// </summary>
public class AtisVariable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AtisVariable"/> class.
    /// </summary>
    /// <param name="find">The find value.</param>
    /// <param name="textReplace">The text replace value.</param>
    /// <param name="voiceReplace">The voice replace value.</param>
    /// <param name="aliases">The aliases.</param>
    /// <returns>A new instance of the <see cref="AtisVariable"/> class.</returns>
    public AtisVariable(string find, string textReplace, string voiceReplace, string[]? aliases = null)
    {
        Find = find;
        TextReplace = textReplace;
        VoiceReplace = voiceReplace;
        Aliases = aliases;
    }

    /// <summary>
    /// Gets or sets the find value.
    /// </summary>
    public string Find { get; set; }

    /// <summary>
    /// Gets or sets the text replace value.
    /// </summary>
    public string TextReplace { get; set; }

    /// <summary>
    /// Gets or sets the voice replace value.
    /// </summary>
    public string VoiceReplace { get; set; }

    /// <summary>
    /// Gets or sets the aliases.
    /// </summary>
    public string[]? Aliases { get; set; }
}
