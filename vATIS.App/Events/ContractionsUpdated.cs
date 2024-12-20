namespace Vatsim.Vatis.Events;

public record ContractionsUpdated(string StationId) : IEvent;