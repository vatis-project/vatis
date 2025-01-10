using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vatsim.Vatis.Config;

public class VatsimStatus
{
    [JsonPropertyName("metar")] public List<string> MetarUrls { get; set; } = [];
}