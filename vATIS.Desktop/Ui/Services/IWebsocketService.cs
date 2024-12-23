using System;
using System.Threading.Tasks;
using SuperSocket.WebSocket.Server;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	void Start();
	Task StartAsync();
	Task StopAsync();
	Task SendAsync(string message);
	public event Action<WebSocketSession, string>? OnGetAtisReceived;

	public event Action? OnGetAllAtisReceived;

	public event Action? OnStarted;

	public event Action? OnStopped;
}
