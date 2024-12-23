using System;
using System.Threading.Tasks;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	void Connect(string uri);
	Task ConnectAsync(string uri);
	Task DisconnectAsync();
	Task SendMessageAsync(string message);
	event Action<string> OnMessageReceived;
}
