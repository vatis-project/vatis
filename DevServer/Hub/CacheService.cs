// <copyright file="CacheService.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using DevServer.Hub.Dto;
using DevServer.Models;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace DevServer.Hub;

/// <summary>
/// Represents a service for caching ATIS data.
/// </summary>
public class CacheService : ICacheService
{
    private readonly IHubContext<ClientHub, IClientHub> _hubContext;
    private readonly Dictionary<string, List<SubscribeDto>> _subscribers = [];
    private readonly ExpiringCacheSet<string, ServerDto>? _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context.</param>
    public CacheService(IHubContext<ClientHub, IClientHub> hubContext)
    {
        _hubContext = hubContext;

        _cache = new ExpiringCacheSet<string, ServerDto>(TimeSpan.FromMinutes(5),
            (key, dto) =>
            {
                // remove expired ATIS
                if (dto.Dto != null)
                {
                    _hubContext.Clients.All.RemoveAtisReceived(dto.Dto);
                    Log.Information("Deleting expired ATIS: " + key);
                }
            });
    }

    /// <inheritdoc/>
    public void AddSubscriber(string connectionId, SubscribeDto dto)
    {
        // Add the subscriber to the list for the current connection
        if (!_subscribers.TryGetValue(connectionId, out var subscribers))
        {
            subscribers = new List<SubscribeDto>();
            _subscribers[connectionId] = subscribers;
        }

        subscribers.Add(dto);
    }

    /// <inheritdoc/>
    public void RemoveSubscriber(string connectionId)
    {
        _subscribers.Remove(connectionId);
    }

    /// <inheritdoc/>
    public AtisHubDto? GetCachedAtis(string stationId, AtisType type)
    {
        var key = $"{stationId}_{type}";
        if (_cache != null && _cache.TryGet(key, out var dto))
        {
            return dto.Dto;
        }

        return null;
    }

    /// <inheritdoc/>
    public void CacheAtis(string key, ServerDto dto)
    {
        _cache?.Set(key, dto);
        foreach (var subscriber in _subscribers)
        {
            if (dto.Dto != null)
            {
                _hubContext.Clients.Client(subscriber.Key).AtisReceived([dto.Dto]);
            }
        }
    }
}
