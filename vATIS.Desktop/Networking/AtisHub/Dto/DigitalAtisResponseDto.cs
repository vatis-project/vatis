using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Networking.AtisHub.Dto;

public class DigitalAtisResponseDto
{
    /// <summary>
    /// The airport identifier.
    /// </summary>
    [JsonPropertyName("airport")]
    public string Airport { get; set; } = string.Empty;

    /// <summary>
    /// The type of ATIS
    /// </summary>
    [JsonPropertyName("type")]
    public string AtisType { get; set; } = string.Empty;

    /// <summary>
    /// The ATIS letter code
    /// </summary>
    [JsonPropertyName("code")]
    public string AtisLetter { get; set; } = string.Empty;

    /// <summary>
    /// The full ATIS message body
    /// </summary>
    [JsonPropertyName("datis")]
    public string Body { get; set; } = string.Empty;
}
