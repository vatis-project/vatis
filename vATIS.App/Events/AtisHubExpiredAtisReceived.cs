using Vatsim.Vatis.Networking.AtisHub;

namespace Vatsim.Vatis.Events;

public record AtisHubExpiredAtisReceived(AtisHubDto Dto) : IEvent;