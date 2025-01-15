// <copyright file="VhfFrequencyFormatBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Provides behavior for formatting VHF frequency input in a <see cref="TextBox"/>.
/// </summary>
public partial class VhfFrequencyFormatBehavior : Behavior<TextBox>
{
    private static readonly Regex s_validInputRegex = FrequencyRegex();

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.AddHandler(InputElement.TextInputEvent, TextInputHandler, RoutingStrategies.Tunnel);
            AssociatedObject.TextChanged += OnTextChanged;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.RemoveHandler(InputElement.TextInputEvent, TextInputHandler);
            AssociatedObject.TextChanged -= OnTextChanged;
        }
    }

    [GeneratedRegex(@"^\d*\.?\d*$")]
    private static partial Regex FrequencyRegex();

    private void TextInputHandler(object? sender, TextInputEventArgs e)
    {
        if (AssociatedObject == null)
        {
            return;
        }

        if (e.Text == null)
        {
            return;
        }

        var textBox = AssociatedObject;
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
        if (AssociatedObject == null)
        {
            return;
        }

        var textBox = AssociatedObject;
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
}
