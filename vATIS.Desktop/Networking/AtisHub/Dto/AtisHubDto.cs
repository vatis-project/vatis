using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub.Dto;

public class AtisHubDto
{
    public string? StationId { get; set; }
    public AtisType AtisType { get; set; }
    public char AtisLetter { get; set; }
    public string? Metar { get; set; }
    public string? Wind { get; set; }
    public string? Altimeter { get; set; }

    public AtisHubDto(string stationId, AtisType atisType, char atisLetter, string? metar, string? wind, string? altimeter)
    {
        StationId = stationId;
        AtisType = atisType;
        AtisLetter = atisLetter;
        Metar = metar;
        Wind = wind;
        Altimeter = altimeter;
    }
}