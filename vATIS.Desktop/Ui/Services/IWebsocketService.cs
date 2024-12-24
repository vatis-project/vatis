using System;
using System.Threading.Tasks;
using Vatsim.Vatis.Events;
using Vatsim.Vatis.Networking;

namespace Vatsim.Vatis.Ui.Services;

public interface IWebsocketService
{
	Task SendAsync(string message);
	Task StartAsync();
	Task StopAsync();

	Task SendNetworkConnectedStatusMessage(NetworkConnectionStatus status);

	public event EventHandler<GetAllAtisReceived> GetAllAtisReceived;
	public event EventHandler<GetAtisReceived> GetAtisReceived;
	public event EventHandler<AcknowledgeAtisUpdateReceived> AcknowledgeAtisUpdateReceived;
}
