using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class FocusOnAttachedToVisualTree : Behavior<TextBox>
{
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        if (AssociatedObject is TextBox textbox)
        {
            textbox.Focus();
        }
    }
}
