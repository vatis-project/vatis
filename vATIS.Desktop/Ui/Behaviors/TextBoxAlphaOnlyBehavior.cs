using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class TextBoxAlphaOnlyBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject?.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
        base.OnDetaching();
    }

    private static void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (e.Text != null && e.Text.Any(c => !char.IsLetter(c)))
        {
            e.Handled = true;
        }
    }
}