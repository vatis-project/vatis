using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

public record PresentWeatherMeta(string Key, string Text, string Spoken) : ReactiveRecord;