using Vatsim.Vatis.Networking.AtisHub;
using Vatsim.Vatis.Networking.AtisHub.Dto;

namespace Vatsim.Vatis.Events;

public record AtisHubExpiredAtisReceived(AtisHubDto Dto) : IEvent;