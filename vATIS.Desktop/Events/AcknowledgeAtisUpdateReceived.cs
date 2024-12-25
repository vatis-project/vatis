using SuperSocket.WebSocket.Server;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Events;

/// <summary>
/// Event arguments for the AcknowledgeAtisUpdateReceived event.
/// </summary>
/// <param name="Session">The client that requested the ATIS information.</param>
/// <param name="Station">The station whose update is acknowledged. If null every station was acknowledged.</param>
/// <param name="AtisType">The ATIS Type whose update is acknowledged.</param>
public record AcknowledgeAtisUpdateReceived(WebSocketSession Session, string? Station, AtisType? AtisType = AtisType.Combined) : IEvent;
