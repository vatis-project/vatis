using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub;

public class SubscribeDto
{
    public SubscribeDto(string stationId, AtisType atisType)
    {
        this.StationId = stationId;
        this.AtisType = atisType;
    }

    public string StationId { get; set; }

    public AtisType AtisType { get; set; }
}