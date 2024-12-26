namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an error message sent over the websocket.
/// </summary>
public class ErrorMessage
{
    /// <summary>
    /// Gets the key identifying the message as an error message.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType { get; } = "error";

    /// <summary>
    /// Gets or sets the error information.
    /// </summary>
    [JsonPropertyName("value")]
    public ErrorValue? Value { get; set; }

    /// <summary>
    /// Represents the value of an error message.
    /// </summary>
    public class ErrorValue
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}