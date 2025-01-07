using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub;

public class DigitalAtisRequestDto
{
    public string Id { get; set; }
    public AtisType AtisType { get; set; }
}