using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub;

public class SubscribeDto
{
    public string StationId { get; set; }
    public AtisType AtisType { get; set; }

    public SubscribeDto(string stationId, AtisType atisType)
    {
        StationId = stationId;
        AtisType = atisType;
    }
}