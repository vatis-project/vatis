using WatsonWebsocket;

namespace Vatsim.Vatis.Events.WebSocket;

/// <summary>
/// Represents an event that is raised by a websocket client to change the profile.
/// </summary>
/// <param name="Session">The websocket client making the request.</param>
/// <param name="ProfileId">The unique profile identifier.</param>
public record GetChangeProfileReceived(ClientMetadata Session, string? ProfileId) : IEvent;
