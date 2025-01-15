// <copyright file="VatsimStatus.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Config;

/// <summary>
/// Represents the VATSIM status configuration, including properties for retrieving METAR URLs.
/// </summary>
public class VatsimStatus
{
    /// <summary>
    /// Gets or sets the list of METAR URLs retrieved from the VATSIM status configuration.
    /// </summary>
    [JsonPropertyName("metar")]
    public List<string> MetarUrls { get; set; } = [];
}
