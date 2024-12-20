namespace Vatsim.Vatis.Events;

public record StartupStatusChanged(string Status) : IEvent;