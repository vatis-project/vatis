using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

public record TransitionLevelMeta(int Low, int High, int Altitude) : ReactiveRecord;