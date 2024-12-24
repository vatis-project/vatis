using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a message to get the ATIS for a specific station.
/// </summary>
class GetAtisMessage
{
	public class GetAtisValue
	{
		[JsonPropertyName("station")]
		/// <summary>
		/// Gets or sets the station to get the ATIS for.
		/// </summary>
		public string? Station { get; set; }
	}

	/// <summary>
	/// Gets or sets the key identifying the message as a get ATIS message.
	/// </summary>
	public string? MessageType { get; set; }

	[JsonPropertyName("value")]
	/// <summary>
	/// Gets or sets the value of the message.
	/// </summary>
	public GetAtisValue? Value { get; set; }
}
