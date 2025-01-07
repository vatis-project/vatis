using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Networking.AtisHub;

public class DigitalAtisResponseDto
{
    [JsonPropertyName("airport")] public string Airport { get; set; }
    [JsonPropertyName("type")] public string AtisType { get; set; }
    [JsonPropertyName("code")] public string AtisLetter { get; set; }
    [JsonPropertyName("datis")] public string Body { get; set; }
}