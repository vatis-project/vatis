namespace Vatsim.Vatis.Events;

public record ExternalAtisConfigChanged(ExternalAtisComponent Component, string Value) : IEvent;

public enum ExternalAtisComponent
{
    Url,
    ArrivalRunways,
    DepartureRunways,
    Approaches,
    Remarks
}
