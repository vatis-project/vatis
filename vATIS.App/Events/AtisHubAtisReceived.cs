using Vatsim.Vatis.Networking.AtisHub;

namespace Vatsim.Vatis.Events;

public record AtisHubAtisReceived(AtisHubDto Dto) : IEvent;