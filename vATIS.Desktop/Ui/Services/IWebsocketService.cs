namespace Vatsim.Vatis.Ui.Services;

using System;
using System.Threading.Tasks;
using SuperSocket.WebSocket.Server;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a WebsocketService.
/// </summary>
public interface IWebsocketService
{
    /// <summary>
    /// Event that is raised when a client acknowledges an ATIS update. The requesting session and the station acknowledged, if specified, is passed as a parameter.
    /// </summary>
    public event EventHandler<AcknowledgeAtisUpdateReceived> AcknowledgeAtisUpdateReceived;

    /// <summary>
    /// Event that is raised when a client requests ATIS information. The requesting session and the station requested, if specified, are passed as parameters.
    /// </summary>
    ///
    public event EventHandler<GetAtisReceived> GetAtisReceived;

    /// <summary>
    /// Sends an ATIS message to a specific session, or to all connected clients if session is null.
    /// </summary>
    /// <param name="session">The session to send the message to.</param>
    /// <param name="value">The value to send.</param>
    /// <returns>A task.</returns>
    Task SendAtisMessage(WebSocketSession? session, AtisMessage.AtisMessageValue value);

    /// <summary>
    /// Starts the WebSocket server.
    /// </summary>
    /// <returns>A task.</returns>
    Task StartAsync();

    /// <summary>
    /// Stops the WebSocket server.
    /// </summary>
    /// <returns>A task.</returns>
    Task StopAsync();
}
