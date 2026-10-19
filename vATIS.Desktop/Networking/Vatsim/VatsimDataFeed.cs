// <copyright file="VatsimDataFeed.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Networking.Vatsim;

/// <summary>
/// The subset of the VATSIM v3 data feed that vATIS reads.
/// </summary>
public class VatsimDataFeed
{
    /// <summary>
    /// Gets or sets the controllers currently connected to the network.
    /// </summary>
    [JsonPropertyName("controllers")]
    public List<VatsimDataFeedController> Controllers { get; set; } = [];
}
