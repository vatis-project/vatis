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
using System.Collections.Concurrent;
using SuperSocket.Server.Host;
using SuperSocket.Server.Abstractions.Session;

namespace Vatsim.Vatis.Ui.Services;

public class WebsocketService : IWebsocketService
{
	private readonly IHost server;
	private readonly ConcurrentDictionary<string, WebSocketSession> sessions = new();

	public event Action<WebSocketSession, string>? GetAtisReceived;
	public event Action? GetAllAtisReceived;
	public event Action? Started;
	public event Action? Stopped;


	public WebsocketService()
	{
		server = WebSocketHostBuilder.Create()
		.UseWebSocketMessageHandler(
				async (session, message) =>
				{
					var result = HandleRequest(session, message);
					if (!string.IsNullOrWhiteSpace(result))
					{
						await session.SendAsync(result);
					}
					else
					{
						await ValueTask.CompletedTask;
					}
				}
		)
		.UseSessionHandler(async (s) =>
		{
			// This method of casing to a WebSocketSession comes from
			// https://github.com/kerryjiang/SuperSocket/blob/e86ace953eb569ade27f06ce554e55a6e8c854c4/test/SuperSocket.Tests/WebSocket/WebSocketBasicTest.cs#L133
			if (s is WebSocketSession session)
			{
				sessions.TryAdd(session.SessionID, session);
			}
			await ValueTask.CompletedTask;
		},
		async (s, e) =>
		{
			sessions.TryRemove(s.SessionID, out _);
			await ValueTask.CompletedTask;
		})
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
	}

	private string HandleRequest(WebSocketSession session, WebSocketPackage message)
	{
		Debug.WriteLine($"Received message {message.Message}");

		var request = JsonSerializer.Deserialize<MessageBase>(message.Message);

		if (request == null || string.IsNullOrWhiteSpace(request.Key))
		{
			return "Invalid request: no Key specified";
		}

		switch (request.Key)
		{
			case "GetAtis":
				if (request.Value is null)
				{
					return "Invalid request: no Value specified";
				}

				var value = JsonSerializer.Deserialize<GetAtisCommand>(request.Value);

				if (string.IsNullOrEmpty(value?.Station))
				{
					return "Invalid request: no Station specified";
				}

				GetAtisReceived?.Invoke(session, value.Station);
				break;
			case "GetAllAtis":
				GetAllAtisReceived?.Invoke();
				break;
			default:
				break;
		}

		return string.Empty;
	}

	public event Action<WebSocketSession, string>? OnGetAtisReceived
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

	public event Action? OnGetAllAtisReceived
	{
		add
		{
			GetAllAtisReceived += value;
		}

		remove
		{
			GetAllAtisReceived -= value;
		}
	}

	public event Action? OnStarted
	{
		add
		{
			Started += value;
		}

		remove
		{
			Started -= value;
		}
	}

	public event Action? OnStopped
	{
		add
		{
			Stopped += value;
		}

		remove
		{
			Stopped -= value;
		}
	}

	public void Start()
	{
		server.Start();
		Started?.Invoke();
	}

	public async Task StartAsync()
	{
		await server.StartAsync();
		Started?.Invoke();
	}

	public async Task StopAsync()
	{
		await server.StopAsync();
		Stopped?.Invoke();
	}

	public Task SendAsync(string message)
	{
		var tasks = new List<Task>();

		foreach (var session in sessions.Values)
		{
			tasks.Add(session.SendAsync(message).AsTask());
		}

		return Task.WhenAll(tasks);
	}
}
