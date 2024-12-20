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
        
        if (IsEnabled)
            AssociatedObject?.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Tunnel);
    }
    
    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (IsEnabled)
            AssociatedObject?.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
    }

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        e.Text = e.Text?.ToUpperInvariant();
    }
}