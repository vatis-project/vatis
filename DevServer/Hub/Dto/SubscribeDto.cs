using DevServer.Models;

namespace DevServer.Hub.Dto;

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