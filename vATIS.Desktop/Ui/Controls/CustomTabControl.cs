using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Controls;

public class CustomTabControl : TabControl
{
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new CustomTabItem();
    }
}