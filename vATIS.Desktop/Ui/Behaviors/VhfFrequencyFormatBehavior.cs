using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public partial class VhfFrequencyFormatBehavior : Behavior<TextBox>
{
    private static readonly Regex ValidInputRegex = FrequencyRegex();
    
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.AddHandler(InputElement.TextInputEvent, TextInputHandler, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            AssociatedObject.TextChanged += OnTextChanged;
        }
    }
    
    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
            AssociatedObject.TextChanged -= OnTextChanged;
        }
    }

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (AssociatedObject == null)
            return;

        if (e.Text == null)
            return;

        var textBox = AssociatedObject;
        var text = textBox.Text ?? string.Empty;
        var input = e.Text;

        string newText;

        if (textBox.SelectedText == text)
        {
            newText = input;
        }
        else
        {
            newText = text.Insert(textBox.CaretIndex, input);
        }

        if (!ValidInputRegex.IsMatch(newText.Replace(".", string.Empty)) || newText.Replace(".", string.Empty).Length > 6)
        {
            e.Handled = true;
            return;
        }

        // Automatically insert period after the third digit
        if (newText.Replace(".", string.Empty).Length == 3 && !text.Contains('.'))
        {
            newText += ".";
        }

        // Handle case when user tries to insert period after an existing one
        if (newText.Contains('.') && input == "." && textBox.CaretIndex > text.IndexOf('.'))
        {
            e.Handled = true;
            return;
        }

        if (newText.Length > 7)
        {
            newText = newText[..7];
        }

        textBox.Text = newText;
        textBox.CaretIndex = newText.Length;
        e.Handled = true;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (AssociatedObject == null)
            return;

        var textBox = AssociatedObject;
        var text = textBox.Text;

        if (string.IsNullOrEmpty(text))
            return;

        if (text.Contains('.') && text.Length > 7)
        {
            text = text[..7];
            textBox.Text = text;
            textBox.CaretIndex = text.Length;
        }
    }

    [GeneratedRegex(@"^\d*\.?\d*$")]
    private static partial Regex FrequencyRegex();
}