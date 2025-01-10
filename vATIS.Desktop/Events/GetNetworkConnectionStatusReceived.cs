using WatsonWebsocket;

namespace Vatsim.Vatis.Events;

/// <summary>
///     Event arguments for the GetNetworkConnectionStatusReceived event.
/// </summary>
/// <param name="Session">The client that requested the network status.</param>
/// <param name="Station">The station to get the network status for. If null all stations are returned.</param>
public record GetNetworkConnectionStatusReceived(ClientMetadata Session, string? Station) : IEvent;