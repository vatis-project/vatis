using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub.Dto;

public class DigitalAtisRequestDto
{
    public string Id { get; set; }
    public AtisType AtisType { get; set; }
}
