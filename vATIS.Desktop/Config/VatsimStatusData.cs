// <copyright file="VatsimStatusData.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Config;

/// <summary>
/// Represents the data feed section of the VATSIM status configuration.
/// </summary>
public class VatsimStatusData
{
    /// <summary>
    /// Gets or sets the URLs of the v3 data feed.
    /// </summary>
    [JsonPropertyName("v3")]
    public List<string> V3 { get; set; } = [];
}
