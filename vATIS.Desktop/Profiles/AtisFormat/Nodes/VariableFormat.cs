// <copyright file="VariableFormat.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Independent text and voice rendering options for a variable.
/// </summary>
public class VariableFormat
{
    /// <summary>
    /// Gets or sets the text ATIS rendering options.
    /// </summary>
    public OutputFormat Text { get; set; } = new();

    /// <summary>
    /// Gets or sets the voice ATIS rendering options.
    /// </summary>
    public OutputFormat Voice { get; set; } = new();
}
