namespace Vatsim.Vatis.Events;

public record NotificationPosted(string Message) : IEvent;