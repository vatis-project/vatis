using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub;

public class AtisHubDto
{
    public AtisHubDto(
        string stationId,
        AtisType atisType,
        char atisLetter,
        string? metar,
        string? wind,
        string? altimeter)
    {
        this.StationId = stationId;
        this.AtisType = atisType;
        this.AtisLetter = atisLetter;
        this.Metar = metar;
        this.Wind = wind;
        this.Altimeter = altimeter;
    }

    public string? StationId { get; set; }

    public AtisType AtisType { get; set; }

    public char AtisLetter { get; set; }

    public string? Metar { get; set; }

    public string? Wind { get; set; }

    public string? Altimeter { get; set; }
}