using System.Text.Json.Nodes;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a message sent over the websocket.
/// </summary>
public class MessageBase
{
  /// <summary>
  /// Gets or sets the key identifying the message.
  /// </summary>
  public string Key { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the value of the message.
  /// </summary>
  public JsonValue? Value { get; set; }
}