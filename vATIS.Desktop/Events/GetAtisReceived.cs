using SuperSocket.WebSocket.Server;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the GetAllAtisReceived event.
/// </summary>
/// <param name="Session">The client that requested the ATIS.</param>
/// <param name="Station">The station requested. If null every station was requested.</param>
public record GetAtisReceived(WebSocketSession Session, string? Station) : IEvent;
