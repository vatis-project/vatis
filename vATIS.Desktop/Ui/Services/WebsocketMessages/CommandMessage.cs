using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

/// <summary>
/// Represents a command sent from a client to the websocket server.
/// </summary>
public class CommandMessage
{
    /// <summary>
    /// Gets or sets the command requested.
    /// </summary>
    [JsonPropertyName("type")]
    public string? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the value of the command.
    /// </summary>
    [JsonPropertyName("value")]
    public CommandMessageValue? Value { get; set; }

    /// <summary>
    /// The values sent with a command, indicating the optional station and
    /// AtisType the command applies to.
    /// </summary>
    public class CommandMessageValue
    {
        /// <summary>
        /// Gets or sets the station the command is for. If null the command is for all stations.
        /// </summary>
        [JsonPropertyName("station")]
        public string? Station { get; set; }

        /// <summary>
        /// Gets or sets the atisType the command is for. Defaults to "Combined".
        /// </summary>
        [JsonPropertyName("atisType")]
        [JsonConverter(typeof(JsonStringEnumConverter<AtisType>))]
        public AtisType AtisType { get; set; } = AtisType.Combined;
    }
}