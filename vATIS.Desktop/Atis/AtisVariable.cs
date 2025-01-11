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
    /// <param name="find">The find string.</param>
    /// <param name="textReplace">The text replace string.</param>
    /// <param name="voiceReplace">The voice replace string.</param>
    /// <param name="aliases">The aliases for the variable.</param>
    public AtisVariable(string find, string textReplace, string voiceReplace, string[]? aliases = null)
    {
        this.Find = find;
        this.TextReplace = textReplace;
        this.VoiceReplace = voiceReplace;
        this.Aliases = aliases;
    }

    /// <summary>
    /// Gets or sets the find string.
    /// </summary>
    public string Find { get; set; }

    /// <summary>
    /// Gets or sets the text replace string.
    /// </summary>
    public string TextReplace { get; set; }

    /// <summary>
    /// Gets or sets the voice replace string.
    /// </summary>
    public string VoiceReplace { get; set; }

    /// <summary>
    /// Gets or sets the aliases for the variable.
    /// </summary>
    public string[]? Aliases { get; set; }
}
