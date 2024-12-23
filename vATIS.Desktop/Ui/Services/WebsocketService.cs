using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Server;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;

namespace Vatsim.Vatis.Ui.Services;

public class WebsocketService : IWebsocketService
{
	private readonly IHost server;

	public event Action<WebSocketSession, string>? GetAtisReceived;

	public WebsocketService()
	{
		server = WebSocketHostBuilder.Create()
		.UseWebSocketMessageHandler(
				async (session, message) =>
				{
					HandleRequest(session, message);
					await session.SendAsync("Hi");
				}
		)
		.ConfigureAppConfiguration((hostCtx, configApp) =>
		{
			configApp.AddInMemoryCollection(new Dictionary<string, string?>
				{
						{ "serverOptions:name", "vATIS" },
						{ "serverOptions:listeners:0:ip", "Any" },
						{ "serverOptions:listeners:0:port", "5092" }
				});
		})
		.Build();

		Debug.WriteLine("Initializing WebsocketService");
	}

	private void HandleRequest(WebSocketSession session, WebSocketPackage message)
	{
		Debug.WriteLine($"Received message {message.Message}");

		var request = JsonSerializer.Deserialize<GetAtisCommand>(message.Message);

		if (request == null || string.IsNullOrWhiteSpace(request.Key) || string.IsNullOrWhiteSpace(request.Station))
		{
			return;
		}

		switch (request.Key)
		{
			case "GetAtis":
				GetAtisReceived?.Invoke(session, request.Station);
				break;
			default:
				break;
		}
	}

	public event Action<WebSocketSession, string> OnGetAtisReceived
	{
		add
		{
			GetAtisReceived += value;
		}

		remove
		{
			GetAtisReceived -= value;
		}
	}

	public void Connect()
	{
		server.Start();
	}

	public async Task ConnectAsync()
	{
		await server.StartAsync();
	}

	public async Task DisconnectAsync()
	{
		await server.StopAsync();
	}

	public Task SendMessageAsync(string message)
	{
	}
}
