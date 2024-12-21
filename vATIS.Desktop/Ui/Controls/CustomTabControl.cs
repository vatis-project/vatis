using Avalonia.Controls;

namespace Vatsim.Vatis.Ui.Controls;

public class CustomTabControl : TabControl
{
    public CustomTabControl()
    {
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    {
        return new CustomTabItem();
    }

    //protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    //{
    //    return NeedsContainer<CustomTabItem>(item, out recycleKey);
    //}

    //protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
    //{
    //    return new CustomTabItem();
    //}
}
