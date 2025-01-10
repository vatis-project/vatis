using Avalonia;
using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Controls;

public class CustomTabItem : TabItem
{
    private static readonly StyledProperty<string> s_atisLetterProperty =
        AvaloniaProperty.Register<CustomTabItem, string>(nameof(AtisLetter));

    private static readonly StyledProperty<bool> s_isConnectedProperty =
        AvaloniaProperty.Register<CustomTabItem, bool>(nameof(IsConnected));

    public string AtisLetter
    {
        get => this.GetValue(s_atisLetterProperty);
        set => this.SetValue(s_atisLetterProperty, value);
    }

    public bool IsConnected
    {
        get => this.GetValue(s_isConnectedProperty);
        set => this.SetValue(s_isConnectedProperty, value);
    }
}