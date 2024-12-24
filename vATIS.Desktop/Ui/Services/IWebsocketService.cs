using System;
using System.Threading.Tasks;
using SuperSocket.WebSocket.Server;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	Task SendAsync(string message);
	Task StartAsync();
	Task StopAsync();

	Task SendAtisMessage(WebSocketSession? session, AtisMessage atis);

	Task SendNetworkConnectedStatusMessage(NetworkConnectionStatus status);

	public event EventHandler<GetAtisReceived> GetAtisReceived;
	public event EventHandler<AcknowledgeAtisUpdateReceived> AcknowledgeAtisUpdateReceived;
}
