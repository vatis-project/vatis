// <copyright file="ContractionMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents metadata for a user-defined contraction variable.
/// </summary>
public class ContractionMeta
{
    /// <summary>
    /// Gets or sets the name of the variable associated with this contraction. Regex contractions are matched
    /// automatically rather than referenced by name, so they have no variable name.
    /// </summary>
    public string? VariableName { get; set; }

    /// <summary>
    /// Gets or sets the text value associated with the contraction.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the spoken value associated with the contraction.
    /// </summary>
    public string? Voice { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="Pattern"/> is a regular expression that is matched against
    /// the ATIS text. When set, <see cref="Text"/> and <see cref="Voice"/> are replacement templates that may reference
    /// capture groups (<c>$1</c>, <c>${name}</c>) and wrap them in functions such as <c>$group($1)</c>.
    /// </summary>
    public bool IsRegex { get; set; }

    /// <summary>
    /// Gets or sets the regular expression matched against the ATIS text when <see cref="IsRegex"/> is set.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the string associated with this contraction.
    /// </summary>
    [Obsolete("Use Text instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? String
    {
        get => null;
        set => Text = value ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the spoken value of the contraction.
    /// </summary>
    [Obsolete("Use Voice instead")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Spoken
    {
        get => null;
        set => Voice = value ?? string.Empty;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ContractionMeta"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="ContractionMeta"/> instance that is a copy of this instance.</returns>
    public ContractionMeta Clone()
    {
        return new ContractionMeta
        {
            VariableName = VariableName,
            Text = Text,
            Voice = Voice,
            IsRegex = IsRegex,
            Pattern = Pattern,
        };
    }
}
