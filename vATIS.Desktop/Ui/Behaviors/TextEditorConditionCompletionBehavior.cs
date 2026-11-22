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
/// space or an opening parenthesis.
/// </summary>
public class TextEditorConditionCompletionBehavior : Behavior<TextEditor>
{
    /// <summary>
    /// Defines the <see cref="Runways"/> property.
    /// </summary>
    public static readonly StyledProperty<ActiveRunways?> RunwaysProperty =
        AvaloniaProperty.Register<TextEditorConditionCompletionBehavior, ActiveRunways?>(nameof(Runways));

    private CompletionWindow? _window;
    private TextEditor? _editor;

    /// <summary>
    /// Gets or sets the runways to suggest after dep:, arr: and rwy:.
    /// </summary>
    public ActiveRunways? Runways
    {
        get => GetValue(RunwaysProperty);
        set => SetValue(RunwaysProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();

        _editor = AssociatedObject ?? throw new InvalidOperationException("AssociatedObject is null");
        _editor.TextArea.TextEntered += OnTextEntered;
        _editor.Options.AllowScrollBelowDocument = false;
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (_editor != null)
        {
            _editor.TextArea.TextEntered -= OnTextEntered;
        }

        _window?.Close();
        base.OnDetaching();
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

        if (char.IsLetter(c) || c is ' ' or '(' or ':')
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

        var text = _editor.Document.Text;
        var caret = _editor.CaretOffset;
        var typed = GetSuggestions(text, caret);
        if (typed.Items.Count == 0)
        {
            return;
        }

        // The list stays open while the word is edited, so it gets everything that fits where the word starts and
        // narrows it as the word changes. A list cut down to what was typed first goes stale when that is deleted.
        var all = GetSuggestions(text, typed.StartOffset);
        var window = new CompletionWindow(_editor.TextArea) { StartOffset = typed.StartOffset };
        foreach (var item in all.Items)
        {
            window.CompletionList.CompletionData.Add(new ConditionCompletionData(item, OnInserted));
        }

        window.Closed += (_, _) => _window = null;
        _window = window;
        window.Show();

        // With nothing typed yet no item is selected, so Enter would do nothing. Start on the first one.
        var partial = text[typed.StartOffset..caret];
        if (partial.Length > 0)
        {
            window.CompletionList.SelectItem(partial);
        }
        else
        {
            window.CompletionList.SelectedItem = window.CompletionList.CompletionData[0];
        }
    }

    private CompletionSuggestionResult GetSuggestions(string text, int caret)
    {
        return ConditionCompletion.GetSuggestions(text, caret, Runways);
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
