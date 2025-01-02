using DevServer.Hub.Dto;
using DevServer.Models;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace DevServer.Hub;

public class CacheService : ICacheService
{
    private readonly IHubContext<ClientHub, IClientHub> _hubContext;
    private readonly Dictionary<string, List<SubscribeDto>> _subscribers = [];
    private readonly ExpiringCacheSet<string, ServerDto>? _cache;

    public CacheService(IHubContext<ClientHub, IClientHub> hubContext)
    {
        _hubContext = hubContext;

        _cache = new ExpiringCacheSet<string, ServerDto>(TimeSpan.FromMinutes(5),
            (key, dto) =>
            {
                // remove expired ATIS
                _hubContext.Clients.All.RemoveAtisReceived(dto.Dto);
                Log.Information("Deleting expired ATIS: " + key);
            });
    }

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

    public void RemoveSubscriber(string connectionId)
    {
        _subscribers.Remove(connectionId);
    }

    public AtisHubDto? GetCachedAtis(string stationId, AtisType type)
    {
        var key = $"{stationId}_{type}";
        if (_cache != null && _cache.TryGet(key, out var dto))
        {
            return dto.Dto;
        }

        return null;
    }

    public void CacheAtis(string key, ServerDto dto)
    {
        _cache?.Set(key, dto);
        foreach (var subscriber in _subscribers)
        {
            _hubContext.Clients.Client(subscriber.Key).AtisReceived([dto.Dto]);
        }
    }
}

public interface ICacheService
{
    void AddSubscriber(string connectionId, SubscribeDto dto);
    void RemoveSubscriber(string connectionId); 
    AtisHubDto? GetCachedAtis(string stationId, AtisType type);
    void CacheAtis(string key, ServerDto dto);
}