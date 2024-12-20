using Avalonia;
using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Controls;

public class CustomTabItem : TabItem
{
    private static readonly StyledProperty<string> AtisLetterProperty =
        AvaloniaProperty.Register<CustomTabItem, string>(nameof(AtisLetter));

    public string AtisLetter
    {
        get => GetValue(AtisLetterProperty);
        set => SetValue(AtisLetterProperty, value);
    }

    private static readonly StyledProperty<bool> IsConnectedProperty =
        AvaloniaProperty.Register<CustomTabItem, bool>(nameof(IsConnected));
    
    public bool IsConnected
    {
        get => GetValue(IsConnectedProperty);
        set => SetValue(IsConnectedProperty, value);
    }
    
    public CustomTabItem()
    {

    }
}
