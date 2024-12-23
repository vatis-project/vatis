using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SuperSocket.WebSocket;
using SuperSocket.WebSocket.Server;
using Vatsim.Vatis.Ui.Services.WebsocketMessages;
using System.Collections.Concurrent;
using SuperSocket.Server.Host;
using System.Text.Json.Nodes;
using SuperSocket.Server.Abstractions;
using Serilog;

namespace Vatsim.Vatis.Ui.Services;

/// <summary>
/// Provides a websocket interface to vATIS for getting ATIS information
/// </summary>
public class WebsocketService : IWebsocketService
{
	private readonly IServer server;

	// A list of connected clients so messages can be broadcast to all connected clients when requested.
	private readonly ConcurrentDictionary<string, WebSocketSession> sessions = new();

	/// <summary>
	/// Event that is raised when a client requests a specific ATIS. The requesting session and the station requested are passed as parameters.
	/// </summary>
	public event Action<WebSocketSession, string>? GetAtisReceived;

	/// <summary>
	/// Event that is raised when a client requests all ATIS messages. The requesting session is passed as a parameter.
	/// </summary>
	public event Action<WebSocketSession>? GetAllAtisReceived;


	public WebsocketService()
	{
		server = WebSocketHostBuilder.Create()
		// Set up handling messages. Any exceptions thrown are put into an ErrorMessage and sent back to the client.
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
		// Save and remove sessions when they connect and disconnect so messages can be broadcast
		// to all connected clients.
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

	/// <summary>
	/// Handles a request from a client. Looks at the Key property to determine the message type
	/// then passes the Value property (if any) to the appropriate handler.
	/// </summary>
	/// <param name="session">The client that sent the message.</param>
	/// <param name="message">The message.</param>
	/// <exception cref="ArgumentException">Thrown if the Key is missing or invalid, or Value properties are missing when required.</exception>
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
				GetAllAtisReceived?.Invoke(session);
				break;
			default:
				throw new ArgumentException($"Invalid request: unknown Key {request.Key}");
		}
	}

	/// <summary>
	/// Event that is raised when a client requests a specific ATIS. The requesting session and the station requested are passed as parameters.
	/// </summary>
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

	/// <summary>
	/// Event that is raised when a client requests all ATIS messages. The requesting session is passed as a parameter.
	/// </summary>
	public event Action<WebSocketSession>? OnGetAllAtisReceived
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

	/// <summary>
	/// Starts the WebSocket server.
	/// </summary>
	/// <returns>A task.</returns>
	public async Task StartAsync()
	{
		try
		{
			await server.StartAsync();
		}
		catch (Exception e)
		{
			Log.Error(e, "Failed to start WebSocket server");
		}
	}

	/// <summary>
	/// Stops the WebSocket server.
	/// </summary>
	/// <returns>A task.</returns>
	public async Task StopAsync()
	{
		try
		{
			await server.StopAsync();
		}
		catch (Exception e)
		{
			Log.Error(e, "Failed to stop WebSocket server");
		}
	}

	/// <summary>
	/// Sends a message to all connected clients.
	/// </summary>
	/// <param name="message">The message to send</param>
	/// <returns>A task.</returns>
	public Task SendAsync(string message)
	{
		var tasks = new List<Task>();

		foreach (var session in sessions.Values)
		{
			tasks.Add(Task.Run(async () =>
			{
				try
				{
					await session.SendAsync(message);
				}
				catch (Exception ex)
				{
					Log.Error(ex, $"Error sending message to session {session.SessionID}");
				}
			}));
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
