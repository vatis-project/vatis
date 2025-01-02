using DevServer.Models;

namespace DevServer.Hub.Dto;

public class AtisHubDto
{
    public string? StationId { get; set; }
    public AtisType AtisType { get; set; }
    public char AtisLetter { get; set; }
    public string? Metar { get; set; }
    public string? Wind { get; set; }
    public string? Altimeter { get; set; }
}