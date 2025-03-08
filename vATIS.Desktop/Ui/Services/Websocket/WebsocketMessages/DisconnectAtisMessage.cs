using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.Websocket.WebsocketMessages;

/// <summary>
/// Represents a message sent over the websocket to disconnect an ATIS.
/// </summary>
public class DisconnectAtisMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "disconnectAtis";

    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    [JsonPropertyName("value")]
    public DisconnectMessagePayload? Payload { get; set; }

    /// <summary>
    /// Represents the message payload.
    /// </summary>
    public class DisconnectMessagePayload
    {
        /// <summary>
        /// Gets or sets the unique station ID.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the station identifier.
        /// </summary>
        [JsonPropertyName("station")]
        public string? Station { get; set; }

        /// <summary>
        /// Gets or sets the station ATIS type.
        /// </summary>
        [JsonPropertyName("atisType")]
        [JsonConverter(typeof(JsonStringEnumConverter<AtisType>))]
        public AtisType? AtisType { get; set; }
    }
}
