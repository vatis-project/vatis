using System.Text.Json.Serialization;

namespace Vatsim.Vatis.TextToSpeech;

public class VoiceMetaData
{
    [JsonPropertyName("name")] public required string Name { get; set; }

    [JsonPropertyName("id")] public required string Id { get; set; }
}