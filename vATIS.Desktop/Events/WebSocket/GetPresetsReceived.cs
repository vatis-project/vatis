using Vatsim.Vatis.Profiles.Models;
using WatsonWebsocket;

namespace Vatsim.Vatis.Events.WebSocket;

/// <summary>
/// Represents an event that is raised when a websocket request is made to get a list of all presets for a specific station.
/// </summary>
/// <param name="Session">The websocket client that made the request.</param>
/// <param name="Station">The station identifier.</param>
/// <param name="AtisType">The station ATIS type. Defaults to Combined if not specified.</param>
public record GetPresetsReceived(ClientMetadata Session, string? Station, AtisType? AtisType = AtisType.Combined) : IEvent;
