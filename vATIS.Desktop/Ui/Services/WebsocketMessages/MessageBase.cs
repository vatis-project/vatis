using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a message sent over the websocket.
/// </summary>
public class MessageBase
{
  [JsonPropertyName("type")]
  /// <summary>
  /// Gets or sets the string identifying the message.
  /// </summary>
  public string MessageType { get; set; } = string.Empty;

  [JsonPropertyName("value")]
  /// <summary>
  /// Gets or sets the value of the message.
  /// </summary>
  public JsonValue? Value { get; set; }
}