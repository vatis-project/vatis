using Vatsim.Vatis.Networking.AtisHub;

namespace Vatsim.Vatis.Events;

public record ConnectionStateChanged(ConnectionState ConnectionState) : IEvent;