using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public class RoutineObservationTimeFormatBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        this.AssociatedObject?.AddHandler(InputElement.TextInputEvent, this.TextInputHandler, RoutingStrategies.Tunnel);
    }

    protected override void OnDetaching()
    {
        this.AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, this.TextInputHandler);
        base.OnDetaching();
    }

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (e.Text != null && e.Text.Any(c => !char.IsNumber(c) && c != ','))
        {
            e.Handled = true;
        }
    }
}