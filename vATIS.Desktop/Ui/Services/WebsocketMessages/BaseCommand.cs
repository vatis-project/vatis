using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

public class BaseMessage
{
	public class BaseMessageValue
	{
		/// <summary>
		/// Gets or sets the station the command is for. If null the command is for all stations.
		/// </summary>
		[JsonPropertyName("station")]
		public string? Station { get; set; }
	}

	[JsonPropertyName("type")]
	/// <summary>
	/// Gets or sets the key identifying the message type.
	/// </summary>
	public string? MessageType { get; set; }

	[JsonPropertyName("value")]
	/// <summary>
	/// Gets or sets the value of the message.
	/// </summary>
	public BaseMessageValue? Value { get; set; }
}