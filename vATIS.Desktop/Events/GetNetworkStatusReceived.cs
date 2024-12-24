using SuperSocket.WebSocket.Server;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the GetNetworkStatusReceived event.
/// </summary>
/// <param name="Session">The client that requested the network status.</param>
/// <param name="Station">The station to get the network status for. If null all stations are returned.</param>
public record GetNetworkStatusReceived(WebSocketSession Session, string? Station) : IEvent;