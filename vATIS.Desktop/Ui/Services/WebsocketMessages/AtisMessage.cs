using System.Text.Json.Serialization;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a message sent over the websocket with ATIS information.
/// </summary>
public class AtisMessage()
{
    /// <summary>
    /// Represents the value of an ATIS message.
    /// </summary>
    public class AtisMessageValue()
    {
        /// <summary>
        /// Gets the network connection status.
        /// </summary>
        [JsonPropertyName("networkConnectionStatus")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public NetworkConnectionStatus? NetworkConnectionStatus { get; set; }

        /// <summary>
        /// Gets the ATIS message text.
        /// </summary>
        [JsonPropertyName("textAtis")]
        public string? TextAtis { get; set; }

        /// <summary>
        /// Gets the station ID of the ATIS message.
        /// </summary>
        [JsonPropertyName("station")]
        public string? Station { get; set; }

        /// <summary>
        /// Gets the type of the ATIS message.
        /// </summary>
        [JsonPropertyName("atisType")]
        public AtisType? AtisType { get; set; }

        /// <summary>
        /// Gets the ATIS letter.
        /// </summary>
        [JsonPropertyName("atisLetter")]
        public char? AtisLetter { get; set; }

        /// <summary>
        /// Gets the METAR used to create the ATIS.
        /// </summary>
        [JsonPropertyName("metar")]
        public string? Metar { get; set; }

        /// <summary>
        /// Gets the current winds.
        /// </summary>
        [JsonPropertyName("wind")]
        public string? Wind { get; set; }

        /// <summary>
        /// Gets the current altimeter.
        /// </summary>
        [JsonPropertyName("altimeter")]
        public string? Altimeter { get; set; }

        /// <summary>
        /// Gets a value indicating whether the ATIS message is new.
        /// </summary>
        [JsonPropertyName("isNewAtis")]
        public bool? IsNewAtis { get; set; }
    }

    /// <summary>
    /// Gets the string identifying the message as an ATIS message.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType { get; } = "atis";

    /// <summary>
    /// Gets or sets the ATIS information.
    /// </summary>
    [JsonPropertyName("value")]
    public AtisMessageValue? Value { get; set; }
}