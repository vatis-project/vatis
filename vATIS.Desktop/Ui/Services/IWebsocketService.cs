using System;
using System.Threading.Tasks;
using SuperSocket.WebSocket.Server;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	void Connect();
	Task ConnectAsync();
	Task DisconnectAsync();
	Task SendMessageAsync(string message);
	public event Action<WebSocketSession, string>? GetAtisReceived;
}
