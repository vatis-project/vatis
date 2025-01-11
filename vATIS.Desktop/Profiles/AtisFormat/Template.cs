// <copyright file="Template.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat;

/// <summary>
/// Represents a template with text and voice components.
/// </summary>
public class Template
{
    /// <summary>
    /// Gets or sets the text component of the template.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the voice component of the template.
    /// </summary>
    public string? Voice { get; set; }
}
