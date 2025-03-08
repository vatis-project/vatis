using Vatsim.Vatis.Ui.Services.Websocket.WebsocketMessages;
using WatsonWebsocket;

namespace Vatsim.Vatis.Events.WebSocket;

/// <summary>
/// Represents an event raised by a websocket client to connect an ATIS.
/// </summary>
/// <param name="Session">The websocket client making the request.</param>
/// <param name="Payload">The message payload.</param>
public record GetConnectAtisReceived(ClientMetadata Session, ConnectAtisMessage.ConnectMessagePayload? Payload) : IEvent;
