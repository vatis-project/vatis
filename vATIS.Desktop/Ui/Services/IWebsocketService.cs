using System;
using System.Threading.Tasks;
using SuperSocket.WebSocket.Server;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	Task SendAtisMessage(WebSocketSession? session, AtisMessage.AtisMessageValue value);
	Task SendNetworkConnectionStatusMessage(WebSocketSession? session, NetworkConnectionStatusMessage.NetworkConnectionStatusValue value);
	Task StartAsync();
	Task StopAsync();

	public event EventHandler<AcknowledgeAtisUpdateReceived> AcknowledgeAtisUpdateReceived;
	public event EventHandler<GetAtisReceived> GetAtisReceived;
	public event EventHandler<GetNetworkConnectionStatusReceived> GetNetworkConnectionStatusReceived;
}
