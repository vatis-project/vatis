// <copyright file="ClientVersionInfo.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace Vatsim.Vatis.Updates;

/// <summary>
/// Represents client version information, including the version and update URL.
/// </summary>
public class ClientVersionInfo
{
    /// <summary>
    /// Gets or sets the version information.
    /// </summary>
    public Version? Version { get; set; }

    /// <summary>
    /// Gets or sets the URL for client updates.
    /// </summary>
    public string? UpdateUrl { get; set; }
}
