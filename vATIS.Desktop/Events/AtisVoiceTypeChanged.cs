namespace Vatsim.Vatis.Events;

public record AtisVoiceTypeChanged(string Id, bool UseTextToSpeech) : IEvent;