using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

public record CloudTypeMeta(string Acronym, string Spoken, string Text) : ReactiveRecord;