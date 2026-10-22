// <copyright file="TextBoxFunctionCompletionBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using Vatsim.Vatis.Atis;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Suggests the pattern replacement functions (<c>$group</c>, <c>$spell</c>, ...) in a <see cref="TextBox"/>
/// when the user types <c>$</c>.
/// </summary>
public class TextBoxFunctionCompletionBehavior : Behavior<TextBox>
{
    private readonly ListBox _list;
    private readonly Border _panel;
    private readonly Popup _popup;
    private int _triggerStart = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextBoxFunctionCompletionBehavior"/> class.
    /// </summary>
    public TextBoxFunctionCompletionBehavior()
    {
        _list = new ListBox
        {
            MaxHeight = 160,
            Focusable = false,
            ItemTemplate = new FuncDataTemplate<(string Name, string Description)>(
                (item, _) => new TextBlock { Text = $"${item.Name}  -  {item.Description}", FontSize = 12, TextWrapping = Avalonia.Media.TextWrapping.Wrap }),
        };
        _list.DoubleTapped += (_, _) => Accept();

        _panel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#323232")),
            BorderBrush = new SolidColorBrush(Color.Parse("#646464")),
            BorderThickness = new Avalonia.Thickness(1),
            Child = _list,
        };

        _popup = new Popup
        {
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            IsLightDismissEnabled = false,
            Focusable = false,
            Child = _panel,
        };
    }

    /// <summary>
    /// Gets or sets a callback that returns whether suggestions should currently be shown, for example only when
    /// the box is in a given state.
    /// </summary>
    public Func<bool>? IsApplicable { get; set; }

    /// <summary>
    /// Finds the <c>$name</c> token being typed that ends at the caret.
    /// </summary>
    /// <param name="text">The text box content.</param>
    /// <param name="caret">The caret position.</param>
    /// <param name="start">The index of the <c>$</c>.</param>
    /// <param name="prefix">The letters typed after the <c>$</c>.</param>
    /// <returns>True if the caret is at the end of a function name being typed.</returns>
    public static bool TryGetTrigger(string? text, int caret, out int start, out string prefix)
    {
        start = -1;
        prefix = string.Empty;
        if (string.IsNullOrEmpty(text) || caret < 1 || caret > text.Length)
        {
            return false;
        }

        var i = caret - 1;
        while (i >= 0 && char.IsAsciiLetter(text[i]))
        {
            i--;
        }

        // Skip a $ that is a group reference inside a function call, e.g. the one in "$spell($".
        if (i < 0 || text[i] != '$' || (i > 0 && (text[i - 1] == '(' || text[i - 1] == '$')))
        {
            return false;
        }

        start = i;
        prefix = text[(i + 1)..caret];
        return true;
    }

    /// <summary>
    /// Inserts a function into the text, replacing the <c>$name</c> being typed.
    /// </summary>
    /// <param name="text">The text box content.</param>
    /// <param name="start">The index of the <c>$</c>.</param>
    /// <param name="caret">The caret position.</param>
    /// <param name="function">The function name to insert.</param>
    /// <returns>The new text and caret position.</returns>
    /// <remarks>
    /// Normally inserts <c>$name($)</c> with the caret inside the brackets, ready for the group number. If a
    /// <c>(</c> already follows, as when the name of an existing <c>$group($1)</c> is being replaced, only the
    /// name is inserted so its brackets and group are kept.
    /// </remarks>
    public static (string Text, int Caret) Complete(string text, int start, int caret, string function)
    {
        caret = Math.Min(caret, text.Length);
        var keepsBrackets = caret < text.Length && text[caret] == '(';
        var insert = keepsBrackets ? $"${function}" : $"${function}($)";

        return (
            text[..start] + insert + text[caret..],
            keepsBrackets ? start + insert.Length : start + insert.Length - 1);
    }

    /// <summary>
    /// Gets the functions whose names start with the given prefix.
    /// </summary>
    /// <param name="prefix">The typed prefix, not case-sensitive.</param>
    /// <returns>The matching functions.</returns>
    public static IReadOnlyList<(string Name, string Description)> Filter(string prefix) =>
        PatternMatcher.Functions
            .Where(f => f.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is not { } textBox)
        {
            return;
        }

        // A popup created in code only gets a host and visual root once it has a logical parent.
        _popup.PlacementTarget = textBox;
        ((ISetLogicalParent)_popup).SetParent(textBox);
        textBox.TextChanged += OnTextChanged;
        textBox.LostFocus += OnLostFocus;
        textBox.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } textBox)
        {
            textBox.TextChanged -= OnTextChanged;
            textBox.LostFocus -= OnLostFocus;
            textBox.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        }

        Close();
        ((ISetLogicalParent)_popup).SetParent(null);
        base.OnDetaching();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (AssociatedObject is not { } textBox)
        {
            return;
        }

        if (IsApplicable?.Invoke() == false ||
            !TryGetTrigger(textBox.Text, textBox.CaretIndex, out var start, out var prefix))
        {
            Close();
            return;
        }

        var matches = Filter(prefix);
        if (matches.Count == 0)
        {
            Close();
            return;
        }

        _triggerStart = start;
        _list.ItemsSource = matches;
        _list.SelectedIndex = 0;

        // Match the width of the text box.
        _panel.Width = textBox.Bounds.Width;
        if (!_popup.IsOpen)
        {
            _popup.Open();
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        // Opening the popup window can briefly move focus, so only close once focus has really left the editor.
        Dispatcher.UIThread.Post(() =>
        {
            if (AssociatedObject is { IsKeyboardFocusWithin: false } && !_list.IsKeyboardFocusWithin)
            {
                Close();
            }
        });
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_popup.IsOpen)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                _list.SelectedIndex = Math.Min(_list.SelectedIndex + 1, _list.ItemCount - 1);
                e.Handled = true;
                break;
            case Key.Up:
                _list.SelectedIndex = Math.Max(_list.SelectedIndex - 1, 0);
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Tab:
                Accept();
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }

    private void Accept()
    {
        if (AssociatedObject is not { } textBox || _triggerStart < 0 ||
            _list.SelectedItem is not ValueTuple<string, string> selected)
        {
            return;
        }

        var (newText, newCaret) = Complete(textBox.Text ?? string.Empty, _triggerStart, textBox.CaretIndex, selected.Item1);

        Close();
        textBox.Text = newText;
        textBox.CaretIndex = newCaret;
    }

    private void Close()
    {
        _popup.IsOpen = false;
        _triggerStart = -1;
    }
}
