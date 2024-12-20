using Vatsim.Vatis.Profiles;
using Vatsim.Vatis.Profiles.Models;

namespace Vatsim.Vatis.Events;
public record NewAtisAcknowledged(AtisStation atis) : IEvent;
