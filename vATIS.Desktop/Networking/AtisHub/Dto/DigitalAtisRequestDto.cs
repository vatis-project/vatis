using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Networking.AtisHub.Dto;

public class DigitalAtisRequestDto
{
    /// <summary>
    /// The identifier for the ATIS request
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The type of ATIS being requested
    /// </summary>
    public AtisType AtisType { get; set; } = AtisType.Combined;
}
