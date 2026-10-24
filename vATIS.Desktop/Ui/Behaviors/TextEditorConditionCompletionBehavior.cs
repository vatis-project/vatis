// <copyright file="TextEditorConditionCompletionBehavior.cs" company="Justin Shannon">
// Copyright (c) Justin Shannon. All rights reserved.
// Licensed under the GPLv3 license. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Utils;
using Vatsim.Vatis.Atis.Extensions;
using Vatsim.Vatis.Ui.Models;

namespace Vatsim.Vatis.Ui.Behaviors;

/// <summary>
/// Suggests keywords while a station or weather condition is typed. The list opens as you type a word, after a
/// space or an opening parenthesis, and on Ctrl+Space.
/// </summary>
public class TextEditorConditionCompletionBehavior : Behavior<TextEditor>
{
    /// <summary>
    /// Defines the <see cref="Kind"/> property.
    /// </summary>
    public static readonly StyledProperty<ConditionCompletionKind> KindProperty =
        AvaloniaProperty.Register<TextEditorConditionCompletionBehavior, ConditionCompletionKind>(nameof(Kind));

    private CompletionWindow? _window;
    private TextEditor? _editor;

    /// <summary>
    /// Gets or sets which kind of condition is being edited.
    /// </summary>
    public ConditionCompletionKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        _editor = AssociatedObject ?? throw new InvalidOperationException("AssociatedObject is null");
        _editor.TextArea.TextEntered += OnTextEntered;
        _editor.TextArea.KeyDown += OnKeyDown;
        _editor.Options.AllowScrollBelowDocument = false;
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (_editor != null)
        {
            _editor.TextArea.TextEntered -= OnTextEntered;
            _editor.TextArea.KeyDown -= OnKeyDown;
        }

        _window?.Close();
        base.OnDetaching();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e is { Key: Key.Space, KeyModifiers: KeyModifiers.Control })
        {
            Show();
            e.Handled = true;
        }
    }

    private void OnTextEntered(object? sender, TextInputEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        var c = e.Text[0];

        // Accepting a suggestion adds a space after it. Closing a group right after one should not leave that space.
        if (c == ')' && _editor != null)
        {
            var paren = _editor.CaretOffset - 1;
            if (paren > 0 && _editor.Document.GetCharAt(paren - 1) == ' ')
            {
                _editor.Document.Remove(paren - 1, 1);
            }
        }

        if (_window != null)
        {
            // Letters and digits keep narrowing the open list. Anything else ends the word, so the list is closed and
            // a fresh one may be offered for what comes next.
            if (char.IsLetterOrDigit(c) || c is '_' or '+' or '-' or '.' or '/')
            {
                return;
            }

            _window.Close();
            _window = null;
        }

        if (char.IsLetter(c) || c is ' ' or '(')
        {
            Show();
        }
    }

    private void Show()
    {
        if (_editor == null || _window != null)
        {
            return;
        }

        var result = Kind == ConditionCompletionKind.Station
            ? StationCompletion.GetSuggestions(_editor.Document.Text, _editor.CaretOffset)
            : WeatherCompletion.GetSuggestions(_editor.Document.Text, _editor.CaretOffset);
        if (result.Items.Count == 0)
        {
            return;
        }

        var window = new CompletionWindow(_editor.TextArea) { StartOffset = result.StartOffset };
        foreach (var item in result.Items)
        {
            window.CompletionList.CompletionData.Add(new ConditionCompletionData(item, OnInserted));
        }

        window.Closed += (_, _) => _window = null;
        _window = window;
        window.Show();

        // With nothing typed yet no item is selected, so Enter would do nothing. Start on the first one.
        window.CompletionList.SelectedItem = window.CompletionList.CompletionData[0];
    }

    private void OnInserted(CompletionSuggestion suggestion)
    {
        // Offer the next piece straight away, e.g. the comparison after a field name.
        if (suggestion.ContinueAfter)
        {
            Dispatcher.UIThread.Post(Show);
        }
    }
}
