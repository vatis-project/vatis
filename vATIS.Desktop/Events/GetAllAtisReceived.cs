using SuperSocket.WebSocket.Server;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the GetAllAtisReceived event.
/// </summary>
/// <param name="Session">The client that requested the ATIS information.</param>
public record GetAllAtisReceived(WebSocketSession Session) : IEvent;