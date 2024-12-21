using System.Text.Json.Serialization;

namespace Vatsim.Vatis.NavData;
public class Navaid
{
    [JsonPropertyName("ID")]
    public required string Id { get; set; }
    public required string Name { get; set; }
}
