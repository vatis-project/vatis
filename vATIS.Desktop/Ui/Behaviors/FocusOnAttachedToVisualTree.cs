using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class FocusOnAttachedToVisualTree : Behavior<TextBox>
{
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        if (this.AssociatedObject is { } textbox)
        {
            textbox.Focus();
        }
    }
}