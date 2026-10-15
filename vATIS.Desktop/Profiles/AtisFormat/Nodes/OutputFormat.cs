// <copyright file="OutputFormat.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat.Nodes;

/// <summary>
/// Rendering options for a variable in a single output (text or voice).
/// </summary>
public class OutputFormat
{
    /// <summary>
    /// Gets or sets how the number is rendered.
    /// </summary>
    public NumberStyle Style { get; set; } = NumberStyle.Default;

    /// <summary>
    /// Gets or sets text appended to the rendered value exactly as written (include a leading space if needed).
    /// </summary>
    public string Suffix { get; set; } = string.Empty;
}
