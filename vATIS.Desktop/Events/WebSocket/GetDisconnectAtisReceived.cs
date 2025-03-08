using Vatsim.Vatis.Ui.Services.Websocket.WebsocketMessages;
using WatsonWebsocket;

namespace Vatsim.Vatis.Events.WebSocket;

/// <summary>
/// Represents an event raised by a websocket client to disconnect an ATIS.
/// </summary>
/// <param name="Session">The websocket client making the request.</param>
/// <param name="Payload">The message payload.</param>
public record GetDisconnectAtisReceived(ClientMetadata Session, DisconnectAtisMessage.DisconnectMessagePayload? Payload) : IEvent;
