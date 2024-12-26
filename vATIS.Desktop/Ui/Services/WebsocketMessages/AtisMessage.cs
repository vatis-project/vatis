namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

using System.Text.Json.Serialization;
using Vatsim.Vatis.Networking;
using Vatsim.Vatis.Profiles.Models;

/// <summary>
/// Represents a message sent over the websocket with ATIS information.
/// </summary>
public class AtisMessage()
{
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

    /// <summary>
    /// Represents the value of an ATIS message.
    /// </summary>
    public class AtisMessageValue()
    {
        /// <summary>
        /// Gets or sets the network connection status.
        /// </summary>
        [JsonPropertyName("networkConnectionStatus")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public NetworkConnectionStatus? NetworkConnectionStatus { get; set; }

        /// <summary>
        /// Gets or sets the ATIS message text.
        /// </summary>
        [JsonPropertyName("textAtis")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TextAtis { get; set; }

        /// <summary>
        /// Gets or sets the station ID of the ATIS message.
        /// </summary>
        [JsonPropertyName("station")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Station { get; set; }

        /// <summary>
        /// Gets or sets the type of the ATIS message.
        /// </summary>
        [JsonPropertyName("atisType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public AtisType? AtisType { get; set; }

        /// <summary>
        /// Gets or sets the ATIS letter.
        /// </summary>
        [JsonPropertyName("atisLetter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public char? AtisLetter { get; set; }

        /// <summary>
        /// Gets or sets the METAR used to create the ATIS.
        /// </summary>
        [JsonPropertyName("metar")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Metar { get; set; }

        /// <summary>
        /// Gets or sets the current winds.
        /// </summary>
        [JsonPropertyName("wind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Wind { get; set; }

        /// <summary>
        /// Gets or sets the current altimeter.
        /// </summary>
        [JsonPropertyName("altimeter")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Altimeter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ATIS message is new.
        /// </summary>
        [JsonPropertyName("isNewAtis")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsNewAtis { get; set; }
    }
}