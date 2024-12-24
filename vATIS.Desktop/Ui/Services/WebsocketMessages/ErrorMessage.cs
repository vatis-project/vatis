using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents an error message sent over the websocket.
/// </summary>
public class ErrorMessage
{
  /// <summary>
  /// Represents the value of an error message.
  /// </summary>
  public class ErrorValue
  {
    [JsonPropertyName("message")]
    /// <summary>
    /// Gets and sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
  }

  [JsonPropertyName("type")]
  /// <summary>
  /// Gets the key identifying the message as an error message.
  /// </summary>
  public string MessageType { get; } = "Error";

  [JsonPropertyName("value")]
  /// <summary>
  /// Gets and sets the error information.
  /// </summary>
  public ErrorValue? Value { get; set; }
}