using System;
using System.Threading.Tasks;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	void Connect();
	Task ConnectAsync();
	Task DisconnectAsync();
	Task SendMessageAsync(string message);
	event Action<string> OnGetAtisReceived;
}
