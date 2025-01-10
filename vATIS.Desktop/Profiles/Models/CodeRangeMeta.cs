// <copyright file="CodeRangeMeta.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a range of codes with a low and high value.
/// </summary>
public class CodeRangeMeta
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeRangeMeta"/> class.
    /// </summary>
    /// <param name="low">The low value of the code range.</param>
    /// <param name="high">The high value of the code range.</param>
    public CodeRangeMeta(char low, char high)
    {
        this.Low = low;
        this.High = high;
    }

    /// <summary>
    /// Gets or sets the low value of the code range.
    /// </summary>
    public char Low { get; set; }

    /// <summary>
    /// Gets or sets the high value of the code range.
    /// </summary>
    public char High { get; set; }
}
