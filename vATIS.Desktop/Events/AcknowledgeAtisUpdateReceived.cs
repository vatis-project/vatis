using SuperSocket.WebSocket.Server;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the AcknowledgeAtisUpdateReceived event.
/// </summary>
/// <param name="Session">The client that requested the ATIS information.</param>
/// <param name="Station">The station whose update is acknowledged. If null every station was acknowledged.</param>
public record AcknowledgeAtisUpdateReceived(WebSocketSession Session, string? Station) : IEvent;
