using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;

namespace Vatsim.Vatis.Ui.Behaviors;

public class TextEditorUpperCaseBehavior : Behavior<TextEditor>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        if (this.IsEnabled)
        {
            this.AssociatedObject?.AddHandler(
                InputElement.TextInputEvent,
                this.TextInputHandler,
                RoutingStrategies.Tunnel);
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (this.IsEnabled)
        {
            this.AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, this.TextInputHandler);
        }
    }

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        e.Text = e.Text?.ToUpperInvariant();
    }
}