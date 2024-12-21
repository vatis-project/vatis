using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

public class StaticDefinition: ReactiveObject
{
    public string Text { get; set; }
    public int Ordinal { get; set; }

    private bool mEnabled;
    public bool Enabled
    {
        get => mEnabled;
        set => this.RaiseAndSetIfChanged(ref mEnabled, value);
    }
    
    public override string? ToString() => Text;

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