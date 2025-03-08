using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Ui.Services.Websocket.WebsocketMessages;

public class AtisPresetMessage
{
    /// <summary>
    /// Gets the string identifying the message type.
    /// </summary>
    [JsonPropertyName("type")]
    public string MessageType => "atisPresets";

    /// <summary>
    /// Gets or sets a list of ATIS preset names.
    /// </summary>
    [JsonPropertyName("presets")]
    public List<string>? Presets { get; set; }
}
