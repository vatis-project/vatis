// <copyright file="BaseFormat.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Vatsim.Vatis.Profiles.AtisFormat;

/// <summary>
/// Represents the base format for ATIS.
/// </summary>
public class BaseFormat
{
    /// <summary>
    /// Gets or sets the template for the ATIS format.
    /// </summary>
    public Template Template { get; set; } = new();
}
