// <copyright file="ICacheService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using DevServer.Hub.Dto;
using DevServer.Models;

namespace DevServer.Hub;

/// <summary>
/// Represents an interface for a service for caching ATIS data.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Adds a subscriber to the cache.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    /// <param name="dto">The subscription data.</param>
    void AddSubscriber(string connectionId, SubscribeDto dto);

    /// <summary>
    /// Removes a subscriber from the cache.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    void RemoveSubscriber(string connectionId);

    /// <summary>
    /// Gets the cached ATIS data.
    /// </summary>
    /// <param name="stationId">The station identifier.</param>
    /// <param name="type">The ATIS type.</param>
    /// <returns>The cached ATIS data.</returns>
    AtisHubDto? GetCachedAtis(string stationId, AtisType type);

    /// <summary>
    /// Caches ATIS data.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="dto">The ATIS data.</param>
    void CacheAtis(string key, ServerDto dto);
}
