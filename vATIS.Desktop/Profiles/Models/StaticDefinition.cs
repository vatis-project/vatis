using ReactiveUI;

namespace Vatsim.Vatis.Profiles.Models;

public class StaticDefinition : ReactiveObject
{
    private bool _enabled;

    public StaticDefinition(string text, int ordinal, bool enabled = true)
    {
        this.Text = text;
        this.Ordinal = ordinal;
        this.Enabled = enabled;
    }

    public string Text { get; set; }

    public int Ordinal { get; set; }

    public bool Enabled
    {
        get => this._enabled;
        set => this.RaiseAndSetIfChanged(ref this._enabled, value);
    }

    public override string ToString()
    {
        return this.Text;
    }

    public StaticDefinition Clone()
    {
        return new StaticDefinition(this.Text, this.Ordinal, this.Enabled);
    }
}