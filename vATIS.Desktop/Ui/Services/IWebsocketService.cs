using System;
using System.Threading.Tasks;
using SuperSocket.WebSocket.Server;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	Task SendAsync(string message);
	Task StartAsync();
	Task StopAsync();

	public event Action<WebSocketSession>? OnGetAllAtisReceived;
	public event Action<WebSocketSession, string>? OnGetAtisReceived;
}
