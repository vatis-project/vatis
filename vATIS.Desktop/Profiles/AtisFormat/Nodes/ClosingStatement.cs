// <copyright file="ClosingStatement.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Represents the closing statement component of the ATIS format.
/// </summary>
public class ClosingStatement : BaseFormat
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClosingStatement"/> class.
    /// </summary>
    public ClosingStatement()
    {
        this.Template = new Template
        {
            Text = "...ADVS YOU HAVE INFO {letter}.",
            Voice = "ADVISE ON INITIAL CONTACT, YOU HAVE INFORMATION {letter|word}",
        };
    }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically include the closing statement.
    /// </summary>
    public bool AutoIncludeClosingStatement { get; set; } = true;

    /// <summary>
    /// Creates a new instance of <see cref="ClosingStatement"/> that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="ClosingStatement"/> instance that is a copy of this instance.</returns>
    public ClosingStatement Clone()
    {
        return (ClosingStatement)this.MemberwiseClone();
    }
}
