using System.Text.Json.Serialization;

namespace Vatsim.Vatis.NavData;

public class Airport
{
    [JsonPropertyName("ID")] public required string Id { get; set; }

    public required string Name { get; set; }

    [JsonPropertyName("Lat")] public double Latitude { get; set; }

    [JsonPropertyName("Lon")] public double Longitude { get; set; }
}