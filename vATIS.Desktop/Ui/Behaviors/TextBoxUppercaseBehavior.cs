using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class TextBoxUppercaseBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.IsEnabled)
        {
            this.AssociatedObject?.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Tunnel);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (this.IsEnabled)
        {
            this.AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
        }
    }

    private static void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        e.Text = e.Text?.ToUpperInvariant();
    }
}