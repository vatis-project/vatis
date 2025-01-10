using Vatsim.Vatis.Weather.Decoder.Entity;

namespace Vatsim.Vatis.Events;

public record MetarReceived(DecodedMetar Metar) : IEvent;