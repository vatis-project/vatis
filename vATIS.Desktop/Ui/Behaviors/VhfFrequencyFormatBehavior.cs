using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

public partial class VhfFrequencyFormatBehavior : Behavior<TextBox>
{
    private static readonly Regex s_validInputRegex = FrequencyRegex();

    protected override void OnAttached()
    {
        base.OnAttached();
        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.AddHandler(
                InputElement.TextInputEvent,
                this.TextInputHandler,
                RoutingStrategies.Tunnel);
            this.AssociatedObject.TextChanged += this.OnTextChanged;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (this.AssociatedObject != null)
        {
            this.AssociatedObject.RemoveHandler(InputElement.TextInputEvent, this.TextInputHandler);
            this.AssociatedObject.TextChanged -= this.OnTextChanged;
        }
    }

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (this.AssociatedObject == null)
        {
            return;
        }

        if (e.Text == null)
        {
            return;
        }

        var textBox = this.AssociatedObject;
        var text = textBox.Text ?? string.Empty;
        var input = e.Text;

        var newText = textBox.SelectedText == text ? input : text.Insert(textBox.CaretIndex, input);

        if (!s_validInputRegex.IsMatch(newText.Replace(".", string.Empty)) ||
            newText.Replace(".", string.Empty).Length > 6)
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
        if (this.AssociatedObject == null)
        {
            return;
        }

        var textBox = this.AssociatedObject;
        var text = textBox.Text;

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

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