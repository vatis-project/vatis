using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

public class AcknowledgeAtisUpdate
{
	public class AcknowledgeAtisUpdateValue
	{
		[JsonPropertyName("station")]
		public string? Station { get; set; }
	}

	[JsonPropertyName("type")]
	/// <summary>
	/// Gets the string identifying the message as an ATIS update acknowledgement.
	/// </summary>
	public string MessageType { get; } = "acknowledgeAtisUpdate";

	[JsonPropertyName("value")]
	/// <summary>
	/// Gets or sets the value of the message.
	/// </summary>
	public AcknowledgeAtisUpdateValue? Value { get; set; }
}