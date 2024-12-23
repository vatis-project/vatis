using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Vatsim.Vatis.Ui.Services;

public class WebsocketService : IWebsocketService
{
	public event Action<string>? OnMessageReceived;

	public WebsocketService()
	{
		Debug.WriteLine("Initializing WebsocketService");
	}

	event Action<string> IWebsocketService.OnMessageReceived
	{
		add
		{
			throw new NotImplementedException();
		}

		remove
		{
			throw new NotImplementedException();
		}
	}

	public void Connect(string uri)
	{
		Debug.WriteLine($"Connecting to {uri}");
	}

	public Task ConnectAsync(string uri)
	{
		Debug.WriteLine($"Connecting to {uri}");
		return Task.CompletedTask;
	}

	public Task DisconnectAsync()
	{
		Debug.WriteLine("Disconnecting");
		return Task.CompletedTask;
	}

	public Task SendMessageAsync(string message)
	{
		Debug.WriteLine($"Sending message {message}");
		return Task.CompletedTask;
	}

	protected virtual void RaiseOnMessageReceived(string message)
	{
		OnMessageReceived?.Invoke(message);
	}
}
