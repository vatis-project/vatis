using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

public class StaticDefinition: ReactiveObject
{
    public string Text { get; set; }
    public int Ordinal { get; set; }

    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set => this.RaiseAndSetIfChanged(ref _enabled, value);
    }

    public override string ToString() => Text;

    public StaticDefinition(string text, int ordinal, bool enabled = true)
    {
        Text = text;
        Ordinal = ordinal;
        Enabled = enabled;
    }

    public StaticDefinition Clone()
    {
        return new StaticDefinition(Text, Ordinal, Enabled);
    }
}
