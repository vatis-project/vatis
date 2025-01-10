using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class SelectAllTextOnFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttachedToVisualTree()
    {
        base.OnAttachedToVisualTree();
        this.AssociatedObject?.AddHandler(InputElement.GotFocusEvent, this.OnGotFocusEvent, RoutingStrategies.Bubble);
    }

    protected override void OnDetachedFromVisualTree()
    {
        base.OnDetachedFromVisualTree();
        this.AssociatedObject?.RemoveHandler(InputElement.GotFocusEvent, this.OnGotFocusEvent);
    }

    private void OnGotFocusEvent(object? sender, GotFocusEventArgs e)
    {
        this.AssociatedObject?.SelectAll();
    }
}