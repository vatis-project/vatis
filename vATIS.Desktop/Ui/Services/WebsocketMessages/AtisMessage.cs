using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Ui.Services.WebsocketMessages;

public class AtisMessage(string stationId, AtisType atisType, char atisLetter, string? metar, string? wind, string? altimeter, bool isPublished) : AtisHubDto(stationId, atisType, atisLetter, metar, wind, altimeter)
{
  public bool IsPublished { get; } = isPublished;
}