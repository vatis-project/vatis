using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

public class AtisPresetsMessage
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
    public List<string> Presets { get; set; }
}
