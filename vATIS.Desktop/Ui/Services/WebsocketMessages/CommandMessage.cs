using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a command sent from a client to the websocket server.
/// </summary>
public class CommandMessage
{
	public class CommandMessageValue
	{
		/// <summary>
		/// Gets or sets the station the command is for. If null the command is for all stations.
		/// </summary>
		[JsonPropertyName("station")]
		public string? Station { get; set; }
	}

	/// <summary>
	/// Gets or sets the command requested.
	/// </summary>
	[JsonPropertyName("type")]
	public string? MessageType { get; set; }

	/// <summary>
	/// Gets or sets the value of the command.
	/// </summary>
	[JsonPropertyName("value")]
	public CommandMessageValue? Value { get; set; }
}