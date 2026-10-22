// <copyright file="IVatsimDataService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Collections.Generic;

namespace Vatsim.Vatis.Networking.Vatsim;

/// <summary>
/// Tracks which ATC positions are online using the VATSIM data feed.
/// </summary>
public interface IVatsimDataService
{
    /// <summary>
    /// Gets the callsigns of the controllers currently online, or null until the feed has been read successfully.
    /// </summary>
    public IReadOnlyCollection<string>? OnlineControllers { get; }

    /// <summary>
    /// Starts polling the data feed. Safe to call repeatedly; the feed is only polled once something needs it.
    /// </summary>
    public void Monitor();
}
