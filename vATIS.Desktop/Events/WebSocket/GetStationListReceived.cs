using WatsonWebsocket;

namespace Vatsim.Vatis.Events.WebSocket;

/// <summary>
/// Represents an event that is raised to request a list of all stations in the loaded profile.
/// </summary>
/// <param name="Session">The websocket client that made the request.</param>
public record GetStationListReceived(ClientMetadata Session) : IEvent;
