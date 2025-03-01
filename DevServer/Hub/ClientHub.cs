// <copyright file="ClientHub.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.Json;
using DevServer.Hub.Dto;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace DevServer.Hub;

/// <summary>
/// Represents a SignalR hub for client-server communication.
/// </summary>
public class ClientHub : Hub<IClientHub>
{
    private static readonly object s_syncLock = new();
    private readonly ICacheService _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientHub"/> class.
    /// </summary>
    /// <param name="cacheService">The cache service.</param>
    public ClientHub(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    /// <inheritdoc/>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Log.Information(exception != null
            ? $"Client disconnected with exception: {exception}"
            : $"Client {Context.ConnectionId} disconnected");

        lock (s_syncLock)
        {
            _cacheService.RemoveSubscriber(Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Client -> Server

    /// <summary>
    /// Subscribes a client to ATIS updates.
    /// </summary>
    /// <param name="dto">The subscription data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SubscribeToAtis(SubscribeDto dto)
    {
        lock (s_syncLock)
        {
            _cacheService.AddSubscriber(Context.ConnectionId, dto);
        }

        var key = $"{dto.StationId}_{dto.AtisType}";
        Log.Debug($"SubscribeToAtis: {key}, {Context.ConnectionId}");

        lock (s_syncLock)
        {
            var atisDto = _cacheService.GetCachedAtis(dto.StationId, dto.AtisType);
            if (atisDto != null)
            {
                Clients.Client(Context.ConnectionId).AtisReceived([atisDto]);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Unsubscribes a client from ATIS updates.
    /// </summary>
    /// <param name="hubDto">The subscription data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PublishAtis(AtisHubDto hubDto)
    {
        var key = $"{hubDto.StationId}_{hubDto.AtisType}";

        hubDto.IsOnline = true;

        var serverDto = new ServerDto
        {
            ConnectionId = Context.ConnectionId,
            Dto = hubDto,
            UpdatedAt = DateTime.UtcNow
        };

        lock (s_syncLock)
        {
            // Upsert ATIS into cache
            _cacheService.CacheAtis(key, serverDto);
            Log.Debug($"PublishAtis: {key}, {Context.ConnectionId}, {JsonSerializer.Serialize(serverDto)}");
        }

        await Clients.Others.AtisReceived([hubDto]);
    }

    /// <summary>
    /// Disconnects an ATIS from the hub.
    /// </summary>
    /// <param name="dto">The dto representing the ATIS to disconnect.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisconnectAtis(AtisHubDto dto)
    {
        var key = $"{dto.StationId}_{dto.AtisType}";

        var serverDto = new ServerDto
        {
            ConnectionId = Context.ConnectionId,
            Dto = new AtisHubDto
            {
                StationId = dto.StationId,
                AtisLetter = dto.AtisLetter,
                AtisType = dto.AtisType,
                IsOnline = false
            },
            UpdatedAt = DateTime.UtcNow
        };

        lock (s_syncLock)
        {
            _cacheService.CacheAtis(key, serverDto);
        }

        await Clients.All.RemoveAtisReceived(dto);

        Log.Debug($"DisconnectAtis: [{Context.ConnectionId}] {key}");
    }
}
