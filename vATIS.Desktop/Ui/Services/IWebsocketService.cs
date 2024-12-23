using System;
using System.Threading.Tasks;
using SuperSocket.WebSocket.Server;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	Task StartAsync();
	Task StopAsync();
	Task SendAsync(string message);
	public event Action<WebSocketSession, string>? OnGetAtisReceived;

	public event Action? OnGetAllAtisReceived;
}
