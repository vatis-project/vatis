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
using Vatsim.Vatis.Events;

namespace Vatsim.Vatis.Ui.Services;

/// <summary>
/// Provides a websocket interface to vATIS
/// </summary>
public class WebsocketService : IWebsocketService
{
	private readonly IServer server;

	// A list of connected clients so messages can be broadcast to all connected clients when requested.
	private readonly ConcurrentDictionary<string, WebSocketSession> sessions = new();

	/// <summary>
	/// Event that is raised when a client requests ATIS information. The requesting session and the station requested, if specified, are passed as parameters.
	/// </summary>
	/// 
	public event EventHandler<GetAtisReceived> GetAtisReceived = delegate { };

	/// <summary>
	/// Event that is raised when a client acknowledges an ATIS update. The requesting session and the station acknowledged, if specified, is passed as a parameter.
	/// </summary>
	public event EventHandler<AcknowledgeAtisUpdateReceived> AcknowledgeAtisUpdateReceived = delegate { };

	/// <summary>
	/// Event that is raised when a client requests network status information. The requesting session and the station requested, if specified, are passed as parameters.
	/// </summary>
	public event EventHandler<GetNetworkStatusReceived> GetNetworkStatusReceived = delegate { };

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
							Value = new ErrorMessage.ErrorValue
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
						{ "serverOptions:listeners:0:port", "49082" }
				});
		})
		.BuildAsServer();
	}

	/// <summary>
	/// Handles a request from a client. Looks at the type property to determine the message type
	/// then fires the appropriate event with the session and station as parameters.
	/// </summary>
	/// <param name="session">The client that sent the message.</param>
	/// <param name="message">The message.</param>
	/// <exception cref="ArgumentException">Thrown if the type is missing or invalid.</exception>
	private void HandleRequest(WebSocketSession session, WebSocketPackage message)
	{
		var request = JsonSerializer.Deserialize<CommandMessage>(message.Message);

		if (request == null || string.IsNullOrWhiteSpace(request.MessageType))
		{
			throw new ArgumentException("Invalid request: no message type specified");
		}

		switch (request.MessageType)
		{
			case "getNetworkStatus":
				GetNetworkStatusReceived?.Invoke(this, new GetNetworkStatusReceived(session, request.Value?.Station));
				break;
			case "getAtis":
				GetAtisReceived?.Invoke(this, new GetAtisReceived(session, request.Value?.Station));
				break;
			case "acknowledgeAtisUpdate":
				AcknowledgeAtisUpdateReceived?.Invoke(this, new AcknowledgeAtisUpdateReceived(session, request.Value?.Station));
				break;
			default:
				throw new ArgumentException($"Invalid request: unknown message type {request.MessageType}");
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
	public async Task SendAsync(string message)
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

		await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Sends a network connection status message to all connected clients.
	/// </summary>
	/// <param name="status">The status to send.</param>
	/// <returns>A task.</returns>
	public async Task SendNetworkConnectedStatusMessage(WebSocketSession? session, NetworkConnectionStatusMessage status)
	{
		if (session is not null)
		{
			await session.SendAsync(JsonSerializer.Serialize(status));
		}
		else
		{
			await SendAsync(JsonSerializer.Serialize(status));
		}
	}

	/// <summary>
	/// Sends an ATIS message to a specific session, or to all connected clients if session is null.
	/// </summary>
	/// <param name="session">The session to send the message to.</param>
	/// <param name="atis">The ATIS to send.</param>
	/// <returns>A task.</returns>
	public async Task SendAtisMessage(WebSocketSession? session, AtisMessage atis)
	{
		if (session is not null)
		{
			await session.SendAsync(JsonSerializer.Serialize(atis));
		}
		else
		{
			await SendAsync(JsonSerializer.Serialize(atis));
		}
	}
}
