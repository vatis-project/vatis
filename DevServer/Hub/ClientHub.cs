using System.Text.Json;
using DevServer.Hub.Dto;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace DevServer.Hub;

public class ClientHub : Hub<IClientHub>
{
    private static readonly object SyncLock = new();
    private readonly ICacheService _cacheService;

    public ClientHub(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Log.Information(exception != null
            ? $"Client disconnected with exception: {exception}"
            : $"Client {Context.ConnectionId} disconnected");
        
        lock (SyncLock)
        {
            _cacheService.RemoveSubscriber(Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
    
    // Client -> Server
    public Task SubscribeToAtis(SubscribeDto dto)
    {
        lock (SyncLock)
        {
            _cacheService.AddSubscriber(Context.ConnectionId, dto);
        }
        
        var key = $"{dto.StationId}_{dto.AtisType}";
        Log.Debug($"SubscribeToAtis: {key}, {Context.ConnectionId}");
        
        lock (SyncLock)
        {
            var atisDto = _cacheService.GetCachedAtis(dto.StationId, dto.AtisType);
            if (atisDto != null)
            {
                Clients.Client(Context.ConnectionId).AtisReceived([atisDto]);
            }
        }

        return Task.CompletedTask;
    }
    
    public async Task PublishAtis(AtisHubDto hubDto)
    {
        var key = $"{hubDto.StationId}_{hubDto.AtisType}";

        var serverDto = new ServerDto
        {
            ConnectionId = Context.ConnectionId,
            Dto = hubDto,
            UpdatedAt = DateTime.UtcNow
        };
        
        lock (SyncLock)
        {
            // Upsert ATIS into cache
            _cacheService.CacheAtis(key, serverDto);
            Log.Debug($"PublishAtis: {key}, {Context.ConnectionId}, {JsonSerializer.Serialize(serverDto)}");
        }

        await Clients.Others.AtisReceived([hubDto]);
    }
}

public interface IClientHub
{
    Task AtisReceived(List<AtisHubDto> dto);
    Task RemoveAtisReceived(AtisHubDto hubDto);
}