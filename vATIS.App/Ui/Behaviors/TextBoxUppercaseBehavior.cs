using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class TextBoxUppercaseBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        if (IsEnabled)
            AssociatedObject?.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (IsEnabled)
            AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
    }

    private static void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        e.Text = e.Text?.ToUpperInvariant();
    }
}