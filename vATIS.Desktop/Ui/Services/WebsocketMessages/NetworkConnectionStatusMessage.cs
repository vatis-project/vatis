using System.Text.Json.Serialization;
using Vatsim.Vatis.Networking;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents an error message sent over the websocket.
/// </summary>
public class NetworkConnectionStatusMessage
{
	/// <summary>
	/// Represents the value of a network connection status message.
	/// </summary>
	public class NetworkConnectionStatusValue
	{
		/// <summary>
		/// Gets and sets the status.
		/// </summary>
		[JsonConverter(typeof(JsonStringEnumConverter))]
		[JsonPropertyName("status")]
		public NetworkConnectionStatus Status { get; set; } = NetworkConnectionStatus.Disconnected;

		/// <summary>
		/// Gets and sets the station.
		/// </summary>
		[JsonPropertyName("station")]
		public string Station { get; set; } = string.Empty;
	}

	[JsonPropertyName("type")]
	/// <summary>
	/// Gets the key identifying the message as network connection status message.
	/// </summary>
	public string MessageType { get; } = "networkConnectionStatus";

	[JsonPropertyName("value")]
	/// <summary>
	/// Gets and sets the error information.
	/// </summary>
	public NetworkConnectionStatusValue? Value { get; set; }
}