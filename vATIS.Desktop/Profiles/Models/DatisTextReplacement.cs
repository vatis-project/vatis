// <copyright file="DatisTextReplacement.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a text replacement rule applied to D-ATIS data before display.
/// </summary>
public class DatisTextReplacement
{
    /// <summary>
    /// Gets or sets the pattern to match in the D-ATIS text.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the replacement text to substitute for the matched pattern.
    /// </summary>
    public string Replacement { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the pattern should be treated as a regular expression.
    /// </summary>
    public bool IsRegex { get; set; }

    /// <summary>
    /// Creates a deep copy of the current <see cref="DatisTextReplacement"/> instance.
    /// </summary>
    /// <returns>A new <see cref="DatisTextReplacement"/> instance that is a copy of this instance.</returns>
    public DatisTextReplacement Clone()
    {
        return new DatisTextReplacement
        {
            Pattern = Pattern,
            Replacement = Replacement,
            IsRegex = IsRegex,
        };
    }
}
