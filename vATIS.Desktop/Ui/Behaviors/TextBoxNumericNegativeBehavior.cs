using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using System.Linq;
using Avalonia.Input;

namespace Vatsim.Vatis.Ui.Behaviors;
public class TextBoxNumericNegativeBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject?.AddHandler(InputElement.TextInputEvent, TextInputHandler, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
        base.OnDetaching();
    }

    private static void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (e.Text != null && e.Text.Any(c => !char.IsNumber(c) && c != '-'))
        {
            e.Handled = true;
        }
    }
}
