namespace Vatsim.Vatis.Profiles.Models;

public class ExternalGenerator
{
    public bool Enabled { get; set; }
    public string? Url { get; set; }
    public string? Arrival { get; set; }
    public string? Departure { get; set; }
    public string? Approaches { get; set; }
    public string? Remarks { get; set; }
}