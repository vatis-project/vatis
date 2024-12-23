using System;
using System.Collections.Generic;
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
using System.Text.Json.Nodes;
using SuperSocket.Server.Abstractions;

namespace Vatsim.Vatis.Ui.Services;

public class WebsocketService : IWebsocketService
{
	private readonly IServer server;
	private readonly ConcurrentDictionary<string, WebSocketSession> sessions = new();

	public event Action<WebSocketSession, string>? GetAtisReceived;
	public event Action? GetAllAtisReceived;


	public WebsocketService()
	{
		server = WebSocketHostBuilder.Create()
		.UseWebSocketMessageHandler(
				async (session, message) =>
				{
					try
					{
						HandleRequest(session, message);
					}
					catch (Exception e)
					{
						var error = new ErrorMessage
						{
							Value = new ErrorValue
							{
								Message = e.Message
							}
						};
						await session.SendAsync(JsonSerializer.Serialize(error));
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
		.BuildAsServer();
	}

	private void HandleRequest(WebSocketSession session, WebSocketPackage message)
	{
		var request = JsonSerializer.Deserialize<MessageBase>(message.Message);

		if (request == null || string.IsNullOrWhiteSpace(request.Key))
		{
			throw new ArgumentException("Invalid request: no Key specified");
		}

		switch (request.Key)
		{
			case "GetAtis":
				HandleGetAtis(session, request.Value);
				break;
			case "GetAllAtis":
				GetAllAtisReceived?.Invoke();
				break;
			default:
				break;
		}
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

	public async Task StartAsync()
	{
		if (server.State is not (ServerState.None or ServerState.Stopped))
		{
			return;
		}

		await server.StartAsync();
	}

	public async Task StopAsync()
	{
		await server.StopAsync();
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

	/// <summary>
	/// Handles requests for a specific station's ATIS.
	/// </summary>
	/// <param name="session">The session that requested the ATIS</param>
	/// <param name="request">The request value</param>
	/// <exception cref="ArgumentException">Thrown when the request is invalid</exception>
	private void HandleGetAtis(WebSocketSession session, JsonValue? request)
	{
		if (request is null)
		{
			throw new ArgumentException("Invalid request: no Value specified");
		}

		var value = JsonSerializer.Deserialize<GetAtisMessage>(request);

		if (string.IsNullOrEmpty(value?.Station))
		{
			throw new ArgumentException("Invalid request: no Station specified");
		}

		GetAtisReceived?.Invoke(session, value.Station);
	}
}
